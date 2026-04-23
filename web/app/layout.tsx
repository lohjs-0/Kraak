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
  metadataBase: new URL(
    process.env.NEXT_PUBLIC_BASE_URL ?? "http://localhost:3000"
  ),
  title: {
    default: "Kraak",
    template: "%s | Kraak",
  },
  description: "Analisador estático de configurações. Detecta credenciais expostas e problemas de segurança antes do deploy.",
  keywords: ["sast", "segurança", "configuração", "secrets", "dotenv"],
  authors: [{ name: "lohjs-0" }],
  openGraph: {
    title: "Kraak",
    description: "Analisador estático de configurações. Detecta credenciais expostas e problemas de segurança antes do deploy.",
    type: "website",
    images: [{ url: "/corvo3.png" }],
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="pt-BR"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <head>
        <link rel="icon" href="/corvo3.png" type="image/png" />
        <link rel="shortcut icon" href="/corvo3.png" type="image/png" />
        <link rel="apple-touch-icon" href="/corvo3.png" />
      </head>
      <body className="min-h-full flex flex-col">{children}</body>
    </html>
  );
}