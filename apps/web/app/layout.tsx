import type { Metadata } from "next";
import "../styles/globals.css";

export const metadata: Metadata = {
  title: "GCCS Compliance Workspace",
  description: "Government contractor compliance operations for small businesses."
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
