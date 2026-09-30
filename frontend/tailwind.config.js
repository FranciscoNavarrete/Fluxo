/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        primary: {
          50:  '#e8eaf6',
          100: '#c5cae9',
          500: '#3f51b5',
          600: '#3949ab',
          700: '#303f9f',
          900: '#1a237e',
        },
        accent: {
          500: '#43a047',
          600: '#388e3c',
        },
      },
    },
  },
  plugins: [],
};

