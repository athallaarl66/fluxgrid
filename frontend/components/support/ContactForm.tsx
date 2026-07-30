"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { apiClient } from "@/lib/api-client";
import { Send } from "lucide-react";

export function ContactForm() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [subject, setSubject] = useState("");
  const [message, setMessage] = useState("");
  const [status, setStatus] = useState<"idle" | "submitting" | "success" | "error">("idle");
  const [errorMsg, setErrorMsg] = useState("");

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setStatus("submitting");
    setErrorMsg("");

    apiClient("/api/support/contact", {
      method: "POST",
      body: JSON.stringify({ name, email, subject, message }),
    })
      .then(() => {
        setStatus("success");
        setName("");
        setEmail("");
        setSubject("");
        setMessage("");
      })
      .catch((err) => {
        setStatus("error");
        setErrorMsg(err.message || "Failed to submit. Please try again.");
      });
  }

  if (status === "success") {
    return (
      <div className="rounded-lg border border-border bg-card p-6 text-center">
        <p className="text-sm font-medium text-foreground">Message sent!</p>
        <p className="mt-1 text-xs text-muted-foreground">
          Thank you for reaching out. We&apos;ll get back to you soon.
        </p>
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="mt-3 border-border text-muted-foreground"
          onClick={() => setStatus("idle")}
        >
          Send another message
        </Button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-3">
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground">Name</label>
          <Input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Your name"
            className="h-8 border-border bg-card text-sm"
            required
          />
        </div>
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground">Email</label>
          <Input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="your@email.com"
            className="h-8 border-border bg-card text-sm"
            required
          />
        </div>
      </div>
      <div className="space-y-1">
        <label className="text-xs font-medium text-muted-foreground">Subject</label>
        <Input
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          placeholder="How can we help?"
          className="h-8 border-border bg-card text-sm"
          required
        />
      </div>
      <div className="space-y-1">
        <label className="text-xs font-medium text-muted-foreground">Message</label>
        <textarea
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          placeholder="Describe your issue or request..."
          rows={4}
          className="w-full rounded border border-border bg-card px-3 py-2 text-sm text-foreground placeholder:text-muted-foreground focus:border-ring focus:ring-1 focus:ring-ring resize-none"
          required
        />
      </div>
      <div className="flex items-center gap-3">
        <Button
          type="submit"
          size="sm"
          disabled={status === "submitting"}
          className="border-accent bg-accent text-accent-foreground font-semibold hover:brightness-[0.95]"
        >
          <Send className="mr-1.5 size-3.5" />
          {status === "submitting" ? "Sending..." : "Send Message"}
        </Button>
        {status === "error" && (
          <span className="text-xs text-destructive">{errorMsg}</span>
        )}
      </div>
    </form>
  );
}
