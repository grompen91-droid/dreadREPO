import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Dread — Atmospheric Horror Overhaul for R.E.P.O.",
  description:
    "Ambient dread, scarier monsters, and a tension system that reads your proximity to danger in real time. A BepInEx mod for R.E.P.O.",
  openGraph: {
    title: "Dread — Atmospheric Horror Overhaul for R.E.P.O.",
    description:
      "Ambient dread, scarier monsters, and a tension system that reads your proximity to danger in real time.",
    type: "website",
  },
};

export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col relative z-10">{children}</body>
    </html>
  );
}
