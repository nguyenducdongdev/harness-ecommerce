import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./app/**/*.{ts,tsx}",
    "./components/**/*.{ts,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        // Brand màu chính — chỉnh theo bộ nhận diện chuỗi cửa hàng
        brand: {
          50: "#fdf5f3",
          100: "#fbe8e3",
          500: "#e05747",
          600: "#c8432f",
          700: "#a83622",
        },
      },
    },
  },
  plugins: [],
};
export default config;
