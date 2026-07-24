import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "@/lib/providers";
import { ThemeInit } from "@/components/ThemeInit";

export const metadata: Metadata = {
  title: "FluxGrid ERP",
  description: "Industrial Modern ERP System",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en" className="h-full antialiased" suppressHydrationWarning>
      <body className="min-h-full flex flex-col" suppressHydrationWarning>
        <ThemeInit />
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
