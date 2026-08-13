import type { Metadata } from "next";
import { Outfit } from "next/font/google";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v15-appRouter";
import { LyoProvider } from "lyo-web-components";
import { ThemeProvider } from "@/components/ThemeProvider";
import "./globals.css";
import "lyo-web-components/styles.css";

const outfit = Outfit({
  subsets: ["latin"],
  variable: "--font-display-loaded",
  display: "swap",
});

export const metadata: Metadata = {
  title: {
    default: "Lyo Comic",
    template: "%s · Lyo Comic",
  },
  description: "Comic library viewer and manager.",
};

const themeBootScript = `
(function(){
  try {
    var t = localStorage.getItem('lyo-comic-theme');
    document.documentElement.setAttribute('data-theme', t === 'light' ? 'light' : 'dark');
  } catch (e) {
    document.documentElement.setAttribute('data-theme', 'dark');
  }
})();
`;

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" data-theme="dark" suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: themeBootScript }} />
      </head>
      <body
        className={outfit.variable}
        style={{
          fontFamily: "var(--font-display-loaded), Outfit, sans-serif",
          ["--font-display" as string]: "var(--font-display-loaded), Outfit, sans-serif",
          ["--font-body" as string]: "var(--font-display-loaded), Outfit, sans-serif",
        }}
      >
        <ThemeProvider>
          <AppRouterCacheProvider>
            <LyoProvider>{children}</LyoProvider>
          </AppRouterCacheProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
