import { Alert, Box, Button, Card, PasswordInput, Stack, Text, TextInput, Title } from '@mantine/core';
import { IconAlertCircle } from '@tabler/icons-react';
import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { ApiError } from '../api/envelope';
import { useAuth } from '../auth/useAuth';

export function LoginPage() {
  const { user, signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const target = (location.state as { from?: string } | null)?.from ?? '/';
  if (user) {
    return <Navigate to={target} replace />;
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await signIn(email, password);
      navigate(target, { replace: true });
    } catch (failure) {
      setError(failure instanceof ApiError ? failure.message : 'Sign-in is unavailable. Try again shortly.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Box mih="100vh" bg="navy.9" px="md" py={64}>
      <Stack align="center" gap="lg">
        <Stack gap={4} align="center">
          <Title order={1} c="white" fz={32}>
            LogistiFlow <Text span c="blue.3" inherit>AI</Text>
          </Title>
          <Text c="gray.4" size="sm">
            Fulfillment operations console
          </Text>
        </Stack>
        <Card w="100%" maw={400} shadow="md">
          <form onSubmit={submit} noValidate>
            <Stack>
              <Title order={2} fz="lg">
                Sign in
              </Title>
              {error && (
                <Alert color="red" variant="light" icon={<IconAlertCircle size={18} />} data-testid="login-error">
                  {error}
                </Alert>
              )}
              <TextInput
                label="E-mail"
                type="email"
                autoComplete="username"
                required
                value={email}
                onChange={(event) => setEmail(event.currentTarget.value)}
                data-testid="login-email"
              />
              <PasswordInput
                label="Password"
                autoComplete="current-password"
                required
                value={password}
                onChange={(event) => setPassword(event.currentTarget.value)}
                data-testid="login-password"
              />
              <Button type="submit" loading={submitting} fullWidth data-testid="login-submit">
                Sign in
              </Button>
            </Stack>
          </form>
        </Card>
      </Stack>
    </Box>
  );
}
