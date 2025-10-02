import {
  AuthContent,
  AuthDivider,
  AuthField,
  AuthForm,
  AuthHeader,
  AuthLink,
  AuthSubmit,
} from './components';
import { AuthProvider } from './context';

// Compound component pattern - compose authentication forms
export const Auth = {
  Provider: AuthProvider,
  Form: AuthForm,
  Header: AuthHeader,
  Content: AuthContent,
  Field: AuthField,
  Submit: AuthSubmit,
  Link: AuthLink,
  Divider: AuthDivider,
};
