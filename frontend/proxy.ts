import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

const publicRoutes = ["/login"];

const skipRoutes = [
  "/api/",
  "/_next/",
  "/favicon.ico",
];

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;

  if (skipRoutes.some((r) => pathname.startsWith(r))) {
    return NextResponse.next();
  }

  if (pathname.endsWith(".svg")) {
    return NextResponse.next();
  }

  const token = request.cookies.get("token")?.value;

  const isPublic = publicRoutes.some((route) => pathname.startsWith(route));

  if (!token && !isPublic) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("redirect", pathname);
    return NextResponse.redirect(loginUrl);
  }

  if (token && isPublic) {
    return NextResponse.redirect(new URL("/dashboard", request.url));
  }

  return NextResponse.next();
}
