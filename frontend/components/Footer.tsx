export function Footer() {
  return (
    <footer className="flex items-center justify-between border-t border-border bg-background px-5 py-3">
      <p className="text-[12px] font-medium text-muted-foreground">
        &copy; 2026 FluxGrid ERP. All rights reserved.
      </p>
      <a
        href="/help"
        className="text-[12px] font-medium text-muted-foreground transition-colors duration-200 hover:text-foreground"
      >
        Help &amp; Documentation
      </a>
    </footer>
  );
}
