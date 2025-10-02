import {
  OrgListProvider,
  OrgListHeader,
  OrgListTitle,
  OrgListActions,
  OrgListContent,
  OrgListGrid,
  OrgListEmpty,
  OrgCard,
  CreateOrgProvider,
  CreateOrgTrigger,
  CreateOrgButton,
  OrgSettingsProvider,
  OrgSettingsHeader,
  OrgSettingsTitle,
  OrgSettingsContent,
  OrgSettingsSection,
  OrgMembersList,
} from './components';

// Compound component pattern - compose organization UI
export const OrgList = {
  Provider: OrgListProvider,
  Header: OrgListHeader,
  Title: OrgListTitle,
  Actions: OrgListActions,
  Content: OrgListContent,
  Grid: OrgListGrid,
  Empty: OrgListEmpty,
  Card: OrgCard,
};

export const CreateOrg = {
  Provider: CreateOrgProvider,
  Trigger: CreateOrgTrigger,
  Button: CreateOrgButton,
};

export const OrgSettings = {
  Provider: OrgSettingsProvider,
  Header: OrgSettingsHeader,
  Title: OrgSettingsTitle,
  Content: OrgSettingsContent,
  Section: OrgSettingsSection,
  MembersList: OrgMembersList,
};
