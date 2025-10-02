import * as Y from 'yjs';
import { Awareness, encodeAwarenessUpdate, applyAwarenessUpdate } from 'y-protocols/awareness';
import {
	HubConnection,
	HubConnectionBuilder,
	HubConnectionState,
	LogLevel,
} from '@microsoft/signalr';
import type { QueryClient } from '@tanstack/react-query';
import { useAuthStore } from '@/stores/auth-store';

export interface CollaborationUser {
	id: string;
	name: string;
	color: string;
}

export interface PresenceState {
	user: CollaborationUser;
	cursor?: {
		anchor: number;
		head: number;
	};
	lastSeen: number;
}

export class CollaborationService {
	private doc: Y.Doc;
	private awareness: Awareness;
	private connection: HubConnection;
	private pageId: string;
	private queryClient: QueryClient;
	private presenceMap = new Map<string, PresenceState>();
	private currentUser: CollaborationUser;
	private updateListeners = new Set<() => void>();
	private synced = false;
	private syncListeners = new Set<() => void>();
	private awarenessListeners = new Set<() => void>();
	private saveDebounceTimer: NodeJS.Timeout | null = null;
	private cachedAwarenessSnapshot: Map<number, unknown> = new Map();
	private wasInitiallyEmpty = false;

	constructor(
		pageId: string,
		currentUser: CollaborationUser,
		queryClient: QueryClient,
		apiUrl: string,
	) {
		this.pageId = pageId;
		this.currentUser = currentUser;
		this.queryClient = queryClient;
		this.doc = new Y.Doc();
		this.awareness = new Awareness(this.doc);

		// Initialize cached awareness snapshot for useSyncExternalStore
		this.cachedAwarenessSnapshot = this.awareness.getStates();

		// Set local awareness state
		this.awareness.setLocalState({
			user: currentUser,
		});

		// Build SignalR connection
		this.connection = new HubConnectionBuilder()
			.withUrl(`${apiUrl}/hubs/document`, {
				accessTokenFactory: () => {
					const token = useAuthStore.getState().token;
					return token || '';
				},
			})
			.withAutomaticReconnect()
			.configureLogging(LogLevel.Information)
			.build();

		this.setupConnectionHandlers();
		this.setupDocumentHandlers();
		this.setupAwarenessHandlers();
	}

	private setupConnectionHandlers() {
		// Handle incoming updates from server
		this.connection.on('ReceiveUpdate', (update: number[]) => {
			const uint8Update = new Uint8Array(update);
			Y.applyUpdate(this.doc, uint8Update);
			this.notifyListeners();
		});

		// Handle incoming awareness updates
		this.connection.on('ReceiveAwarenessUpdate', (update: number[]) => {
			const uint8Update = new Uint8Array(update);
			// Apply update without triggering local change event
			applyAwarenessUpdate(this.awareness, uint8Update, 'remote');
		});

		// Handle presence updates
		this.connection.on(
			'PresenceUpdate',
			(userId: string, presence: PresenceState | null) => {
				if (presence === null) {
					this.presenceMap.delete(userId);
				} else {
					this.presenceMap.set(userId, presence);
				}

				// Push to React Query cache
				this.queryClient.setQueryData(
					['presence', this.pageId],
					Array.from(this.presenceMap.values()),
				);
			},
		);

		// Handle current users list (sent when joining)
		this.connection.on('CurrentUsers', (users: PresenceState[]) => {
			// Initialize presence map with current users
			users.forEach((user) => {
				this.presenceMap.set(user.user.id, user);
			});

			// Push to React Query cache
			this.queryClient.setQueryData(
				['presence', this.pageId],
				Array.from(this.presenceMap.values()),
			);
		});

		// Handle user joined
		this.connection.on('UserJoined', (userData: { userId: string; userName: string }) => {
			// User joined - we'll get their presence via PresenceUpdate
		});

		// Handle user left
		this.connection.on('UserLeft', (connectionId: string) => {
			// User left - presence will be cleaned up via PresenceUpdate
		});

		// Handle initial sync
		this.connection.on('InitialState', (state: number[]) => {
			// Track whether server had content
			this.wasInitiallyEmpty = !state || state.length === 0;

			if (state && state.length > 0) {
				const uint8State = new Uint8Array(state);
				Y.applyUpdate(this.doc, uint8State);
			}
			this.synced = true;
			this.notifyListeners();
			this.notifySyncListeners();
		});

		// Handle reconnection
		this.connection.onreconnected(async () => {
			await this.joinDocument();
		});
	}

	private setupDocumentHandlers() {
		// Send local updates to server
		this.doc.on('update', (update: Uint8Array, origin: unknown) => {
			// Don't send updates that came from the server
			if (origin !== this) {
				this.sendUpdate(update);
				// Trigger debounced save - save after 3 seconds of inactivity
				this.debouncedSave();
			}
		});
	}

	private setupAwarenessHandlers() {
		// Send local awareness changes to server
		this.awareness.on('change', ({ added, updated, removed }) => {
			// Only broadcast if local state changed (not remote updates)
			const changedClients = added.concat(updated).concat(removed);
			if (changedClients.includes(this.awareness.clientID)) {
				this.broadcastAwarenessUpdate();
			}

			// Update cached snapshot when awareness changes
			this.cachedAwarenessSnapshot = this.awareness.getStates();

			// Notify React components
			this.notifyAwarenessListeners();
		});
	}

	private async broadcastAwarenessUpdate() {
		if (this.connection.state === HubConnectionState.Connected) {
			try {
				const update = Array.from(
					encodeAwarenessUpdate(this.awareness, [
						this.awareness.clientID,
					]),
				);
				await this.connection.invoke(
					'SendAwarenessUpdate',
					this.pageId,
					update,
				);
			} catch (error) {
				console.error('Failed to send awareness update:', error);
			}
		}
	}

	private async sendUpdate(update: Uint8Array) {
		if (this.connection.state === HubConnectionState.Connected) {
			try {
				await this.connection.invoke(
					'SendUpdate',
					this.pageId,
					Array.from(update),
				);
			} catch (error) {
				console.error('Failed to send update:', error);
			}
		}
	}

	async connect() {
		// Don't connect if already connected, connecting, or not fully disconnected
		if (
			this.connection.state === HubConnectionState.Connected ||
			this.connection.state === HubConnectionState.Connecting ||
			this.connection.state === HubConnectionState.Reconnecting ||
			this.connection.state === HubConnectionState.Disconnecting
		) {
			return;
		}

		try {
			await this.connection.start();
			await this.joinDocument();
		} catch (error) {
			console.error('Failed to start connection:', error);
			throw error;
		}
	}

	private debouncedSave() {
		// Clear existing timer
		if (this.saveDebounceTimer) {
			clearTimeout(this.saveDebounceTimer);
		}

		// Save after 3 seconds of inactivity
		this.saveDebounceTimer = setTimeout(() => {
			this.saveSnapshot();
		}, 3000);
	}

	private async saveSnapshot() {
		if (this.connection.state !== HubConnectionState.Connected) {
			return;
		}

		try {
			// Get the current document state as a snapshot
			const stateVector = Y.encodeStateAsUpdate(this.doc);
			const stateArray = Array.from(stateVector);

			// Send to server for persistence
			await this.connection.invoke('SaveSnapshot', this.pageId, stateArray);
		} catch (error) {
			console.error('Failed to save snapshot:', error);
		}
	}

	private async joinDocument() {
		try {
			await this.connection.invoke('JoinDocument', this.pageId);

			// Announce presence with proper shape
			const presence: PresenceState = {
				user: this.currentUser,
				lastSeen: Date.now(),
			};
			await this.connection.invoke(
				'UpdatePresence',
				this.pageId,
				presence,
			);
		} catch (error) {
			console.error('Failed to join document:', error);
		}
	}

	async disconnect() {
		// Clear debounce timer
		if (this.saveDebounceTimer) {
			clearTimeout(this.saveDebounceTimer);
			this.saveDebounceTimer = null;
		}

		// Save final snapshot before disconnecting
		await this.saveSnapshot();

		if (this.connection.state === HubConnectionState.Connected) {
			try {
				await this.connection.invoke('LeaveDocument', this.pageId);
			} catch (error) {
				console.error('Failed to leave document:', error);
			}
		}

		await this.connection.stop();
		this.awareness.destroy();
		this.doc.destroy();
	}

	async updateCursor(anchor: number, head: number) {
		if (this.connection.state === HubConnectionState.Connected) {
			const presence: PresenceState = {
				user: this.currentUser,
				cursor: { anchor, head },
				lastSeen: Date.now(),
			};

			try {
				await this.connection.invoke(
					'UpdatePresence',
					this.pageId,
					presence,
				);
			} catch (error) {
				console.error('Failed to update presence:', error);
			}
		}
	}

	getDoc() {
		return this.doc;
	}

	getAwareness() {
		return this.awareness;
	}

	// Provider-like wrapper for TipTap CollaborationCursor
	getProvider() {
		return {
			awareness: this.awareness,
			doc: this.doc,
		};
	}

	isSynced() {
		return this.synced;
	}

	wasServerEmpty() {
		return this.wasInitiallyEmpty;
	}

	getPresence(): PresenceState[] {
		return Array.from(this.presenceMap.values()).filter(
			(p) => p.user.id !== this.currentUser.id,
		);
	}

	onUpdate(callback: () => void) {
		this.updateListeners.add(callback);
		return () => {
			this.updateListeners.delete(callback);
		};
	}

	private notifyListeners() {
		this.updateListeners.forEach((callback) => callback());
	}

	private notifySyncListeners() {
		this.syncListeners.forEach((callback) => callback());
	}

	onSyncStatusChange(callback: () => void) {
		this.syncListeners.add(callback);
		return () => {
			this.syncListeners.delete(callback);
		};
	}

	private notifyAwarenessListeners() {
		this.awarenessListeners.forEach((callback) => callback());
	}

	onAwarenessChange(callback: () => void) {
		this.awarenessListeners.add(callback);
		return () => {
			this.awarenessListeners.delete(callback);
		};
	}

	// For useSyncExternalStore - awareness snapshot
	getAwarenessSnapshot() {
		return this.cachedAwarenessSnapshot;
	}

	subscribeToAwareness(callback: () => void) {
		this.awareness.on('change', callback);
		return () => {
			this.awareness.off('change', callback);
		};
	}
}

// Stable color mapping for users
const USER_COLORS = [
	'#ff6b6b',
	'#4ecdc4',
	'#45b7d1',
	'#96ceb4',
	'#ffeaa7',
	'#dfe6e9',
	'#74b9ff',
	'#a29bfe',
	'#fd79a8',
	'#fdcb6e',
];

export function getUserColor(userId: string): string {
	const hash = userId.split('').reduce((acc, char) => {
		return char.charCodeAt(0) + ((acc << 5) - acc);
	}, 0);

	return USER_COLORS[Math.abs(hash) % USER_COLORS.length];
}
