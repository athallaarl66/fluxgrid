import { NextRequest, NextResponse } from "next/server";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5020";

export async function POST(request: NextRequest) {
  const token = request.cookies.get("token")?.value;
  if (!token) {
    return NextResponse.json({ message: "Not authenticated" }, { status: 401 });
  }

  try {
    const { oldPassword, newPassword, confirmNewPassword } =
      await request.json();

    const response = await fetch(`${API_BASE_URL}/api/auth/change-password`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ oldPassword, newPassword, confirmNewPassword }),
      signal: AbortSignal.timeout(5000),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      return NextResponse.json(
        { message: error.message || "Password change failed" },
        { status: response.status },
      );
    }

    const data = await response.json();
    const res = NextResponse.json({ success: true });

    // Set new token as httpOnly cookie after password change
    res.cookies.set("token", data.token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "strict",
      path: "/",
      maxAge: 60 * 60,
    });

    return res;
  } catch (err) {
    const message =
      err instanceof TypeError
        ? "Cannot connect to server. Make sure the backend is running on port 5020."
        : "An unexpected error occurred.";
    return NextResponse.json({ message }, { status: 503 });
  }
}
