import type { Metadata } from "next";
import { Syne, Source_Serif_4, IBM_Plex_Mono } from "next/font/google";
import { SiteHeader } from "@/components/SiteHeader";
import { SiteFooter } from "@/components/SiteFooter";
import { ThemeProvider } from "@/components/ThemeProvider";
import "./globals.css";

const syne = Syne({
  subsets: ["latin"],
  variable: "--font-display-loaded",
  display: "swap",
});

const sourceSerif = Source_Serif_4({
  subsets: ["latin"],
  variable: "--font-body-loaded",
  display: "swap",
});

const plexMono = IBM_Plex_Mono({
  subsets: ["latin"],
  weight: ["400", "500"],
  variable: "--font-mono-loaded",
  display: "swap",
});

export const metadata: Metadata = {
  title: {
    default: "Matthew Wherry · Lyo",
    template: "%s · Matthew Wherry · Lyo",
  },
  description:
    "Matthew Wherry — Lyo (Library for Your Organization): a .NET toolkit for APIs, file handling, encryption, compression, jobs, and cross-cutting infrastructure.",
};

const themeBootScript = `
(function(){
  try {
    var t = localStorage.getItem('lyo-theme');
    document.documentElement.setAttribute('data-theme', t === 'dark' ? 'dark' : 'light');
  } catch (e) {
    document.documentElement.setAttribute('data-theme', 'light');
  }
})();
`;

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en" data-theme="light" suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: themeBootScript }} />
      </head>
      <body
        className={`${syne.variable} ${sourceSerif.variable} ${plexMono.variable}`}
        style={{
          fontFamily: "var(--font-body-loaded), var(--font-body)",
          ["--font-display" as string]: "var(--font-display-loaded), Syne, sans-serif",
          ["--font-body" as string]: "var(--font-body-loaded), 'Source Serif 4', Georgia, serif",
          ["--font-mono" as string]: "var(--font-mono-loaded), 'IBM Plex Mono', monospace",
        }}
      >
        <ThemeProvider>
          <SiteHeader />
          <main className="site-main">{children}</main>
          <SiteFooter />
        </ThemeProvider>
      </body>
    </html>
  );
}
