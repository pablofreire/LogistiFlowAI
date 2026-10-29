import { ActionIcon, AppShell, Avatar, Burger, Group, NavLink, Stack, Text, Tooltip, useMantineColorScheme } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconBuildingWarehouse, IconLayoutDashboard, IconLogout, IconMoon, IconPackage, IconSun } from '@tabler/icons-react';
import { Link, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';

const navigation = [
  { to: '/', label: 'Dashboard', icon: IconLayoutDashboard, testId: 'nav-dashboard' },
  { to: '/orders', label: 'Orders', icon: IconPackage, testId: 'nav-orders' },
  { to: '/inventory', label: 'Inventory', icon: IconBuildingWarehouse, testId: 'nav-inventory' },
];

export function AppLayout() {
  const [opened, { toggle, close }] = useDisclosure();
  const { user, signOut } = useAuth();
  const { colorScheme, toggleColorScheme } = useMantineColorScheme();
  const location = useLocation();

  const isActive = (to: string) => (to === '/' ? location.pathname === '/' : location.pathname.startsWith(to));

  return (
    <AppShell header={{ height: 60 }} navbar={{ width: 232, breakpoint: 'sm', collapsed: { mobile: !opened } }} padding="lg">
      <AppShell.Header bg="navy.9" c="white" withBorder={false}>
        <Group h="100%" px="md" justify="space-between" wrap="nowrap">
          <Group gap="sm" wrap="nowrap">
            <Burger opened={opened} onClick={toggle} hiddenFrom="sm" size="sm" color="white" aria-label="Toggle navigation" />
            <Text fw={700} fz="lg" data-testid="brand">
              LogistiFlow <Text span c="blue.3" inherit>AI</Text>
            </Text>
          </Group>
          <Group gap="xs" wrap="nowrap">
            <Tooltip label={colorScheme === 'dark' ? 'Light mode' : 'Dark mode'}>
              <ActionIcon variant="subtle" color="gray.0" onClick={toggleColorScheme} aria-label="Toggle color scheme" data-testid="theme-toggle">
                {colorScheme === 'dark' ? <IconSun size={18} /> : <IconMoon size={18} />}
              </ActionIcon>
            </Tooltip>
            <Group gap={8} visibleFrom="xs" wrap="nowrap">
              <Avatar size={30} color="blue" radius="xl">
                {user?.displayName.charAt(0)}
              </Avatar>
              <Stack gap={0}>
                <Text size="sm" fw={600} lh={1.2} data-testid="current-user">
                  {user?.displayName}
                </Text>
                <Text size="xs" c="gray.4" lh={1.2} data-testid="current-role">
                  {user?.role}
                </Text>
              </Stack>
            </Group>
            <Tooltip label="Sign out">
              <ActionIcon variant="subtle" color="gray.0" onClick={signOut} aria-label="Sign out" data-testid="sign-out">
                <IconLogout size={18} />
              </ActionIcon>
            </Tooltip>
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="sm">
        <nav aria-label="Main">
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              component={Link}
              to={item.to}
              label={item.label}
              leftSection={<item.icon size={18} stroke={1.6} />}
              active={isActive(item.to)}
              onClick={close}
              data-testid={item.testId}
              mb={2}
            />
          ))}
        </nav>
      </AppShell.Navbar>

      <AppShell.Main bg="var(--mantine-color-body)">
        <Outlet />
      </AppShell.Main>
    </AppShell>
  );
}
