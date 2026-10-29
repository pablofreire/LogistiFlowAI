import { createTheme, type MantineColorsTuple } from '@mantine/core';

// Navy for chrome and emphasis, blue for actions; neutral grays everywhere else.
const navy: MantineColorsTuple = [
  '#eef3fa',
  '#d9e2f0',
  '#aec2e0',
  '#80a0cf',
  '#5a83c1',
  '#4171b9',
  '#3367b6',
  '#25569f',
  '#1c4c8f',
  '#0b1f3a',
];

export const theme = createTheme({
  primaryColor: 'blue',
  primaryShade: { light: 7, dark: 5 },
  colors: { navy },
  fontFamily: 'Inter, "Segoe UI", system-ui, -apple-system, sans-serif',
  headings: { fontFamily: 'Inter, "Segoe UI", system-ui, -apple-system, sans-serif', fontWeight: '650' },
  defaultRadius: 'md',
  components: {
    Card: { defaultProps: { withBorder: true, radius: 'md', padding: 'lg' } },
    Paper: { defaultProps: { radius: 'md' } },
  },
});
