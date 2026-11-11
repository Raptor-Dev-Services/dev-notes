import tseslint from 'typescript-eslint';
import boundaries from 'eslint-plugin-boundaries';

export default tseslint.config(
  { ignores: ['dist', 'node_modules'] },
  ...tseslint.configs.recommended,
  {
    files: ['src/**/*.{ts,tsx}'],
    plugins: { boundaries },
    settings: {
      'import/resolver': { node: { extensions: ['.ts', '.tsx', '.js'] } },
      'boundaries/elements': [
        { type: 'app', pattern: 'src/app' },
        { type: 'feature', pattern: 'src/features/*', capture: ['nombre'] },
        { type: 'shared', pattern: 'src/shared' },
      ],
    },
    rules: {
      'boundaries/dependencies': [
        'error',
        {
          default: 'disallow',
          policies: [
            { from: { element: { type: 'app' } }, allow: { to: { element: { type: 'feature', fileInternalPath: 'index.ts' } } } },
            { from: { element: { type: 'app' } }, allow: { to: { element: { type: 'shared' } } } },
            { from: { element: { type: 'feature' } }, allow: { to: { element: { type: 'shared' } } } },
            { from: { element: { type: 'feature' } }, allow: { to: { element: { type: 'feature', captured: { nombre: '{{ from.element.captured.nombre }}' } } } } },
            { from: { element: { type: 'shared' } }, allow: { to: { element: { type: 'shared' } } } },
          ],
        },
      ],
    },
  },
);
