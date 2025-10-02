/**
 * Schema-derived types - Single source of truth from OpenAPI
 * These types are derived from the auto-generated schema.ts
 */

import type { components } from './schema';

// Derive DTOs from schema components
export type UserDto = components['schemas']['UserDto'];
export type PageDto = components['schemas']['GetPageResult'];
export type PageSummary = components['schemas']['PageSummary'];
export type BlockDto = components['schemas']['BlockDto'];
export type OrganizationDto = components['schemas']['OrganizationDto'];
export type MemberDto = components['schemas']['MemberDto'];

// Request/Response types
export type CreatePageRequest = components['schemas']['CreatePageRequest'];
export type CreatePageResult = components['schemas']['CreatePageResult'];
export type UpdatePageTitleRequest = components['schemas']['UpdatePageTitleRequest'];
export type GetOrganizationResult = components['schemas']['GetOrganizationResult'];
export type ListPagesResult = components['schemas']['ListPagesResult'];

// Auth types
export type AuthResponseDto = components['schemas']['AuthResponseDto'];
export type RegisterRequestDto = components['schemas']['RegisterRequestDto'];
export type LoginRequestDto = components['schemas']['LoginRequestDto'];

// Block operations
export type AddBlockRequest = components['schemas']['AddBlockRequest'];
export type AddBlockResult = components['schemas']['AddBlockResult'];
export type UpdateBlockRequest = components['schemas']['UpdateBlockRequest'];

// Organization operations
export type CreateOrganizationRequest = components['schemas']['CreateOrganizationRequest'];
export type CreateOrganizationResult = components['schemas']['CreateOrganizationResult'];
export type InviteMemberRequest = components['schemas']['InviteMemberRequest'];
export type InviteMemberResult = components['schemas']['InviteMemberResult'];
