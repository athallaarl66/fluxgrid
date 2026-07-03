import { NextRequest, NextResponse } from "next/server";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5020";

export async function POST(request: NextRequest) {
  try {
    const { username, password } = await request.json();

    const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
      signal: AbortSignal.timeout(5000),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      return NextResponse.json(
        {
          code: error.code || "INVALID_CREDENTIALS",
          message: error.message || "Invalid credentials",
        },
        { status: response.status },
      );
    }

    const data = await response.json();
    const res = NextResponse.json({ success: true });

    res.cookies.set("token", data.token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      maxAge: 60 * 60,
    });

    return res;
  } catch (err) {
    const message =
      err instanceof TypeError
        ? "Cannot connect to server. Make sure the backend is running on port 5020."
        : "An unexpected error occurred.";

    return NextResponse.json({ code: "SERVER_ERROR", message }, { status: 503 });
  }
}
