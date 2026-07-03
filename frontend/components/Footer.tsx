export function Footer() {
  return (
    <footer className="flex items-center justify-between border-t border-[#e5debf] bg-[#fdf8f5] px-5 py-3">
      <p className="text-[12px] font-medium text-[#7a776d]">
        &copy; 2024 FluxGrid ERP. All rights reserved.
      </p>
      <a
        href="/help"
        className="text-[12px] font-medium text-[#7a776d] transition-colors duration-200 hover:text-[#1c1b1a]"
      >
        Help &amp; Documentation
      </a>
    </footer>
  );
}
