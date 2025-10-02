import { useEditor, EditorContent } from '@tiptap/react';
import {
  StarterKit,
  Placeholder,
  TiptapUnderline,
  TiptapLink,
  TiptapImage,
  TaskList,
  TaskItem,
} from 'novel';
import { Table } from '@tiptap/extension-table';
import { TableRow } from '@tiptap/extension-table-row';
import { TableCell } from '@tiptap/extension-table-cell';
import { TableHeader } from '@tiptap/extension-table-header';
import DragHandle from '@tiptap/extension-drag-handle-react';
import { Collaboration } from '@tiptap/extension-collaboration';
import { slashCommand } from './slash-command-novel';
import { ImageUpload } from './extensions/image-upload';
import { useImageUpload } from '@/hooks/use-image-upload';
import { GripVertical } from 'lucide-react';
import type { CollaborationService, CollaborationUser, PresenceState } from '@/lib/collaboration';
import { useMemo, useEffect, useLayoutEffect, useRef, useSyncExternalStore, useCallback, useState } from 'react';

interface BlockEditorProps {
  content?: string;
  editable?: boolean;
  placeholder?: string;
  collaborationService?: CollaborationService;
  user?: CollaborationUser;
  pageId?: string;
  orgId?: string;
}

export function BlockEditor({
  content = '',
  editable = true,
  collaborationService,
  user,
  pageId,
  orgId,
}: BlockEditorProps) {
  const initialContentLoadedRef = useRef(false);
  const initialContentRef = useRef(content);
  const prevCollaborationServiceRef = useRef(collaborationService);
  const { mutateAsync: uploadImage } = useImageUpload();

  // Stabilize uploadImage callback for memoization
  const uploadImageCallback = useCallback(async (file: File) => {
    return uploadImage({ file, pageId: pageId!, orgId: orgId! });
  }, [uploadImage, pageId, orgId]);


  // Reset when collaboration service changes (new page/mount)
  useLayoutEffect(() => {
    if (prevCollaborationServiceRef.current !== collaborationService) {
      initialContentLoadedRef.current = false;
      initialContentRef.current = content;
      prevCollaborationServiceRef.current = collaborationService;
    }
  }, [collaborationService, content]);

  // Memoize extensions to prevent recreation on every render
  const extensions = useMemo(() => {
    const exts = [
      StarterKit.configure({
        heading: {
          levels: [1, 2, 3],
        },
        // Disable history when using collaboration
        history: !collaborationService,
      }),
      Placeholder.configure({
        placeholder: ({ node }) => {
          if (node.type.name === 'heading') {
            return `Heading ${node.attrs.level}`;
          }
          return "Type '/' for commands or just start typing...";
        },
      }),
      slashCommand,
      TiptapUnderline,
      TiptapLink.configure({
        openOnClick: false,
      }),
      TiptapImage,
      TaskList,
      TaskItem.configure({
        nested: true,
      }),
      Table.configure({
        resizable: true,
      }),
      TableRow,
      TableCell,
      TableHeader,
    ];

    // Add image upload extension if pageId and orgId are provided
    if (pageId && orgId) {
      exts.push(
        ImageUpload.configure({
          onUpload: async (file: File) => {
            const result = await uploadImageCallback(file);
            return result.fileUrl;
          },
        })
      );
    }

    // Add collaboration extension if service provided
    if (collaborationService) {
      const doc = collaborationService.getDoc();

      if (doc) {
        // Get the XML fragment explicitly - this is what ySyncPlugin needs
        const fragment = doc.getXmlFragment('default');

        // Add Collaboration for document sync
        exts.push(
          Collaboration.configure({
            fragment: fragment,
          }),
        );
      }
    }

    return exts;
  }, [collaborationService, user, pageId, orgId, uploadImageCallback]);

  const editor = useEditor({
    extensions,
    // Only set initial content when NOT using collaboration
    content: !collaborationService && content ? JSON.parse(content) : undefined,
    editable,
    editorProps: {
      attributes: {
        class: 'focus:outline-none min-h-[500px] px-16 py-8 prose prose-sm sm:prose lg:prose-lg dark:prose-invert max-w-none',
      },
    },
  });

  // Load initial content into Yjs when using collaboration (only once, and only if no CRDT state exists)
  useLayoutEffect(() => {
    if (!editor || !collaborationService) return;

    // Wait for CollaborationService to be synced before checking if we need to load initial content
    const checkSyncAndLoad = () => {
      if (initialContentLoadedRef.current) return;

      // Only load DB content if:
      // 1. We're synced with the server
      // 2. The editor is completely empty (no CRDT state from other clients)
      // 3. We have initial content from the database
      if (collaborationService.isSynced()) {
        // Mark as loaded immediately to prevent race conditions
        initialContentLoadedRef.current = true;

        // Only load DB content if:
        // 1. Editor is empty
        // 2. Server had no CRDT state (wasServerEmpty)
        // 3. We have DB content to load
        if (editor.isEmpty &&
            collaborationService.wasServerEmpty() &&
            initialContentRef.current) {
          try {
            const initialContent = JSON.parse(initialContentRef.current);
            // Only load if the parsed content actually has content
            if (initialContent && (initialContent.content?.length > 0 || Object.keys(initialContent).length > 0)) {
              editor.commands.setContent(initialContent, false);
            }
          } catch (e) {
            console.error('Failed to load initial content:', e);
          }
        }
        // If server had content, it's already in the editor via Yjs sync
      }
    };

    // Check immediately and subscribe to sync status changes
    checkSyncAndLoad();
    const unsubscribe = collaborationService.onSyncStatusChange(checkSyncAndLoad);

    return () => {
      unsubscribe?.();
    };
  }, [editor, collaborationService]);

  // Stable empty Map for when there's no collaboration
  const emptyMapRef = useRef(new Map());

  // Memoize getSnapshot to prevent infinite loops
  const getSnapshot = useCallback(() => {
    if (!collaborationService) return emptyMapRef.current;
    return collaborationService.getAwarenessSnapshot();
  }, [collaborationService]);

  // Subscribe to awareness changes for cursor visualization
  const awarenessStates = useSyncExternalStore(
    (callback) => {
      if (!collaborationService) return () => {};
      return collaborationService.subscribeToAwareness(callback);
    },
    getSnapshot,
    () => emptyMapRef.current
  );

  // Track local cursor position and broadcast via awareness
  useEffect(() => {
    if (!editor || !collaborationService) return;

    const awareness = collaborationService.getAwareness();

    const handleSelectionUpdate = () => {
      const { anchor, head } = editor.state.selection;

      // Set cursor in awareness (syncs instantly via Yjs)
      awareness.setLocalStateField('cursor', { anchor, head });

      // Also update presence for backward compatibility
      collaborationService.updateCursor(anchor, head);
    };

    editor.on('selectionUpdate', handleSelectionUpdate);
    editor.on('transaction', handleSelectionUpdate);

    // Also handle clicks explicitly
    editor.view.dom.addEventListener('click', handleSelectionUpdate);
    editor.view.dom.addEventListener('focus', handleSelectionUpdate);

    return () => {
      editor.off('selectionUpdate', handleSelectionUpdate);
      editor.off('transaction', handleSelectionUpdate);
      editor.view.dom.removeEventListener('click', handleSelectionUpdate);
      editor.view.dom.removeEventListener('focus', handleSelectionUpdate);

      // Clear cursor from awareness on unmount (broadcasts null to other clients)
      awareness.setLocalStateField('cursor', null);
    };
  }, [editor, collaborationService]);

  // Track cursor and selection positions for remote users
  const [remoteCursors, setRemoteCursors] = useState<Array<{
    clientId: number;
    userName: string;
    color: string;
    cursor: { top: number; left: number; height: number } | null;
    selection: Array<{ top: number; left: number; width: number; height: number }> | null;
  }>>([]);

  // Update cursor positions from awareness states
  useEffect(() => {
    if (!editor || !collaborationService) return;

    const awareness = collaborationService.getAwareness();
    const localClientId = awareness.clientID;
    let rafId: number | null = null;
    let lastUpdateTime = 0;
    let previousCursorsJson = '';
    const MIN_UPDATE_INTERVAL = 100; // ms - throttle to max 10fps

    const calculateCursorPositions = () => {
      const now = performance.now();

      // Throttle: minimum 100ms between updates
      if (now - lastUpdateTime < MIN_UPDATE_INTERVAL) {
        return;
      }
      lastUpdateTime = now;

      const states = Array.from(awarenessStates.entries());

      // Memoize editor rect - only recalculate when needed
      const editorRect = editor.view.dom.getBoundingClientRect();

      const cursors = states
        .filter(([clientId]) => clientId !== localClientId)
        .map(([clientId, state]: [number, any]) => {
          const user = state?.user;
          const cursorData = state?.cursor;

          if (!user || !cursorData) {
            return null;
          }

          try {
            const { anchor, head } = cursorData;

            // Cursor position (head of selection)
            const headCoords = editor.view.coordsAtPos(head);
            const cursor = {
              top: headCoords.top - editorRect.top,
              left: headCoords.left - editorRect.left,
              height: headCoords.bottom - headCoords.top || 20,
            };

            // Selection ranges (if anchor !== head)
            let selection: Array<{ top: number; left: number; width: number; height: number }> | null = null;
            if (anchor !== head) {
              const from = Math.min(anchor, head);
              const to = Math.max(anchor, head);

              // Get selection rectangles for multi-line selections
              const rects: Array<{ top: number; left: number; width: number; height: number }> = [];

              try {
                const fromCoords = editor.view.coordsAtPos(from);
                const toCoords = editor.view.coordsAtPos(to);

                // Simple case: single line selection
                if (Math.abs(fromCoords.top - toCoords.top) < 5) {
                  rects.push({
                    top: fromCoords.top - editorRect.top,
                    left: fromCoords.left - editorRect.left,
                    width: toCoords.right - fromCoords.left,
                    height: fromCoords.bottom - fromCoords.top,
                  });
                } else {
                  // Multi-line: approximate with rectangles
                  // First line
                  rects.push({
                    top: fromCoords.top - editorRect.top,
                    left: fromCoords.left - editorRect.left,
                    width: editorRect.width - (fromCoords.left - editorRect.left),
                    height: fromCoords.bottom - fromCoords.top,
                  });

                  // Last line
                  rects.push({
                    top: toCoords.top - editorRect.top,
                    left: 0,
                    width: toCoords.left - editorRect.left,
                    height: toCoords.bottom - toCoords.top,
                  });
                }
              } catch (e) {
                // Selection might be partially out of range
              }

              selection = rects.length > 0 ? rects : null;
            }

            return {
              clientId,
              userName: user.name || 'Anonymous',
              color: user.color || '#000',
              cursor,
              selection,
            };
          } catch (e) {
            // Position might be out of range
            return null;
          }
        })
        .filter((c): c is NonNullable<typeof c> => c !== null);

      // Only update state if cursors actually changed
      const cursorsJson = JSON.stringify(cursors);
      if (cursorsJson !== previousCursorsJson) {
        previousCursorsJson = cursorsJson;
        setRemoteCursors(cursors);
      }
    };

    // Schedule update with debouncing
    const scheduleUpdate = () => {
      if (rafId) {
        cancelAnimationFrame(rafId);
      }

      rafId = requestAnimationFrame(() => {
        calculateCursorPositions();
        rafId = null;
      });
    };

    // Update when awareness changes
    const unsubscribe = collaborationService.subscribeToAwareness(scheduleUpdate);

    // Update on editor changes (document might shift positions)
    editor.on('transaction', scheduleUpdate);

    // Initial update
    calculateCursorPositions();

    return () => {
      unsubscribe();
      editor.off('transaction', scheduleUpdate);
      if (rafId) {
        cancelAnimationFrame(rafId);
      }
    };
  }, [editor, collaborationService, awarenessStates]);

  if (!editor) {
    return null;
  }

  // Get remote users from awareness (exclude local user)
  const remoteUsers = Array.from(awarenessStates.entries())
    .filter(([clientId]) => {
      if (!collaborationService) return false;
      return clientId !== collaborationService.getAwareness().clientID;
    })
    .map(([clientId, state]) => ({
      clientId,
      user: (state as any)?.user || {},
    }));

  return (
    <div className="w-full cursor-text relative">
      <DragHandle editor={editor}>
        <div className="flex items-center gap-2 rounded-md bg-background border border-muted p-1 cursor-grab active:cursor-grabbing">
          <GripVertical className="h-4 w-4 text-muted-foreground" />
        </div>
      </DragHandle>
      <EditorContent editor={editor} />

      {/* Render remote cursors and selections */}
      {remoteCursors.map((remote) => (
        <div key={remote.clientId}>
          {/* Selection highlight */}
          {remote.selection?.map((rect, i) => (
            <div
              key={`${remote.clientId}-selection-${i}`}
              className="absolute pointer-events-none will-change-transform transition-transform duration-100 ease-out"
              style={{
                top: 0,
                left: 0,
                transform: `translate(${rect.left}px, ${rect.top}px)`,
                width: rect.width,
                height: rect.height,
                backgroundColor: remote.color,
                opacity: 0.3,
              }}
            />
          ))}

          {/* Cursor */}
          {remote.cursor && (
            <div
              className="absolute pointer-events-none will-change-transform transition-transform duration-100 ease-out z-10"
              style={{
                top: 0,
                left: 0,
                transform: `translate(${remote.cursor.left}px, ${remote.cursor.top}px)`,
                height: remote.cursor.height,
              }}
            >
              <div
                className="w-0.5 h-full"
                style={{ backgroundColor: remote.color }}
              />
              <div
                className="absolute -top-5 left-0 text-xs px-1.5 py-0.5 rounded whitespace-nowrap text-white font-medium"
                style={{ backgroundColor: remote.color }}
              >
                {remote.userName}
              </div>
            </div>
          )}
        </div>
      ))}

      {/* Show active collaborators indicator */}
      {remoteUsers.length > 0 && (
        <div className="absolute top-2 right-2 flex items-center gap-1 text-xs text-muted-foreground bg-background/80 backdrop-blur-sm px-2 py-1 rounded-md border">
          <div className="flex -space-x-2">
            {remoteUsers.slice(0, 3).map(({ clientId, user }) => (
              <div
                key={clientId}
                className="w-6 h-6 rounded-full border-2 border-background flex items-center justify-center text-white font-semibold text-[10px]"
                style={{ backgroundColor: user.color || '#000' }}
                title={user.name || 'Anonymous'}
              >
                {(user.name || 'A')[0].toUpperCase()}
              </div>
            ))}
          </div>
          {remoteUsers.length > 3 && (
            <span className="ml-1">+{remoteUsers.length - 3}</span>
          )}
        </div>
      )}
    </div>
  );
}
