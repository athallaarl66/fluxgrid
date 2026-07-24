"use client";

import { HelpCircle, Info, MessageSquare } from "lucide-react";
import { FaqAccordion } from "@/components/support/FaqAccordion";
import { ContactForm } from "@/components/support/ContactForm";
import faqData from "@/data/faq.json";

export default function SupportPage() {
  return (
    <div className="p-5 space-y-6 animate-fade-in max-w-3xl">
      <div className="flex items-center gap-3">
        <div className="flex size-10 items-center justify-center rounded-lg bg-accent">
          <HelpCircle className="size-5 text-accent-foreground" />
        </div>
        <div>
          <h1 className="text-2xl font-semibold leading-tight tracking-tight text-foreground">
            Support
          </h1>
          <p className="mt-0.5 text-sm text-muted-foreground">
            Get help and find answers to your questions
          </p>
        </div>
      </div>

      <div className="space-y-4">
        <div className="rounded-xl border border-border bg-card p-5">
          <div className="flex items-center gap-2 mb-3">
            <HelpCircle className="size-4 text-muted-foreground" />
            <h2 className="text-sm font-semibold text-foreground">Frequently Asked Questions</h2>
          </div>
          <FaqAccordion items={faqData} />
        </div>

        <div className="rounded-xl border border-border bg-card p-5">
          <div className="flex items-center gap-2 mb-3">
            <Info className="size-4 text-muted-foreground" />
            <h2 className="text-sm font-semibold text-foreground">About FluxGrid ERP</h2>
          </div>
          <div className="space-y-1.5 text-xs text-muted-foreground">
            <p><span className="font-medium text-foreground">Version:</span> 1.0.0</p>
            <p><span className="font-medium text-foreground">Stack:</span> .NET 8 + Next.js 16 + PostgreSQL</p>
            <p><span className="font-medium text-foreground">Modules:</span> WMS, Finance, HR & Payroll</p>
          </div>
        </div>

        <div className="rounded-xl border border-border bg-card p-5">
          <div className="flex items-center gap-2 mb-3">
            <MessageSquare className="size-4 text-muted-foreground" />
            <h2 className="text-sm font-semibold text-foreground">Contact Support</h2>
          </div>
          <ContactForm />
        </div>
      </div>
    </div>
  );
}
