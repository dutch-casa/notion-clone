import type { FormEvent, ReactNode } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useAuthContext } from './context';

interface AuthFormProps {
  children: ReactNode;
  className?: string;
}

export function AuthForm({ children, className }: AuthFormProps) {
  const { form } = useAuthContext();

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    form.handleSubmit();
  };

  return (
    <Card className={className}>
      <form onSubmit={handleSubmit}>{children}</form>
    </Card>
  );
}

interface AuthHeaderProps {
  children: ReactNode;
  description?: string;
}

export function AuthHeader({ children, description }: AuthHeaderProps) {
  return (
    <CardHeader>
      <CardTitle className="text-2xl">{children}</CardTitle>
      {description && <CardDescription>{description}</CardDescription>}
    </CardHeader>
  );
}

export function AuthContent({ children }: { children: ReactNode }) {
  return <CardContent className="space-y-4">{children}</CardContent>;
}

interface AuthFieldProps {
  name: string;
  label: string;
  type?: 'text' | 'email' | 'password';
  placeholder?: string;
}

export function AuthField({ name, label, type = 'text', placeholder }: AuthFieldProps) {
  const { form } = useAuthContext();

  // Determine autocomplete attribute based on field name and type
  const getAutocomplete = () => {
    if (type === 'password') return 'current-password';
    if (type === 'email') return 'email';
    if (name === 'name') return 'name';
    return undefined;
  };

  return (
    <form.Field name={name}>
      {(field: any) => (
        <div className="space-y-2">
          <Label htmlFor={field.name}>{label}</Label>
          <Input
            id={field.name}
            type={type}
            placeholder={placeholder}
            autoComplete={getAutocomplete()}
            value={field.state.value as string}
            onChange={(e) => field.handleChange(e.target.value)}
            onBlur={field.handleBlur}
          />
          {field.state.meta.errors.length > 0 && (
            <p className="text-sm text-destructive">{field.state.meta.errors[0]}</p>
          )}
        </div>
      )}
    </form.Field>
  );
}

interface AuthSubmitProps {
  children: ReactNode;
}

export function AuthSubmit({ children }: AuthSubmitProps) {
  const { isSubmitting } = useAuthContext();

  return (
    <Button type="submit" className="w-full" disabled={isSubmitting}>
      {isSubmitting ? 'Loading...' : children}
    </Button>
  );
}

interface AuthLinkProps {
  children: ReactNode;
  onClick: () => void;
}

export function AuthLink({ children, onClick }: AuthLinkProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="text-sm text-muted-foreground hover:text-foreground transition-colors"
    >
      {children}
    </button>
  );
}

export function AuthDivider() {
  return (
    <div className="relative">
      <div className="absolute inset-0 flex items-center">
        <span className="w-full border-t" />
      </div>
      <div className="relative flex justify-center text-xs uppercase">
        <span className="bg-card px-2 text-muted-foreground">Or</span>
      </div>
    </div>
  );
}
