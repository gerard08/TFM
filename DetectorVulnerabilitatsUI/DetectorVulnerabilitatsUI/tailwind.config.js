/** @type {import('tailwindcss').Config} */
module.exports = {
  // AQUESTA LÍNIA ÉS LA CLAU:
  content: [
    "./src/**/*.{html,ts}"
  ],
  theme: {
    extend: {}, // <--- Important: Posa les coses aquí, no a 'theme' directament
  },
  plugins: [],
}
