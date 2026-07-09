import { AuthProvider } from "@/lib/auth-context";
import { Sidebar } from "@/components/Sidebar";
import { Header } from "@/components/Header";
import { Footer } from "@/components/Footer";
import { ToastProvider } from "@/components/ui/toast";

export default function HRLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <AuthProvider>
      <ToastProvider>
        <div className="flex min-h-screen bg-background">
          <Sidebar />
          <div className="flex flex-1 flex-col ml-[260px]">
            <Header />
            <main className="flex-1">{children}</main>
            <Footer />
          </div>
        </div>
      </ToastProvider>
    </AuthProvider>
  );
}
