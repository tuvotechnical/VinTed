// VinTed Edge Function: sepay-webhook
// Deploy: supabase functions deploy sepay-webhook --no-verify-jwt
// IMPORTANT: Deploy with --no-verify-jwt because SePay sends API Key, not JWT
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const PLAN_DURATIONS: Record<string, number> = {
  daily: 1,
  monthly: 30,
  yearly: 365,
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response(JSON.stringify({ success: true }), {
      status: 200, headers: { "Content-Type": "application/json" },
    });
  }

  try {
    // Verify SePay API Key
    const authHeader = req.headers.get("Authorization") || "";
    const expectedKey = `Apikey ${Deno.env.get("SEPAY_WEBHOOK_KEY")}`;

    if (authHeader !== expectedKey) {
      console.error("Unauthorized webhook call");
      return new Response(JSON.stringify({ success: false, error: "Unauthorized" }), {
        status: 401, headers: { "Content-Type": "application/json" },
      });
    }

    const tx = await req.json();
    console.log("SePay webhook received:", JSON.stringify(tx));

    // Only process incoming transfers
    if (tx.transferType !== "in") {
      return new Response(JSON.stringify({ success: true }), {
        status: 200, headers: { "Content-Type": "application/json" },
      });
    }

    const supabase = createClient(
      Deno.env.get("SUPABASE_URL")!,
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!
    );

    // Idempotency check: skip if transaction already processed
    const { data: existingTx } = await supabase
      .from("sepay_transactions")
      .select("id")
      .eq("id", tx.id)
      .single();

    if (existingTx) {
      console.log("Transaction already processed:", tx.id);
      return new Response(JSON.stringify({ success: true }), {
        status: 200, headers: { "Content-Type": "application/json" },
      });
    }

    // Find matching pending order by code or content containing order_code
    const orderCode = tx.code || "";
    const content = tx.content || "";

    let order = null;

    if (orderCode) {
      const { data } = await supabase
        .from("orders")
        .select("*")
        .eq("order_code", orderCode.toUpperCase())
        .eq("status", "pending")
        .single();
      order = data;
    }

    if (!order && content) {
      // Try to extract VINTED code from content
      const match = content.toUpperCase().match(/VINTED[A-Z0-9]{6}/);
      if (match) {
        const { data } = await supabase
          .from("orders")
          .select("*")
          .eq("order_code", match[0])
          .eq("status", "pending")
          .single();
        order = data;
      }
    }

    // Save transaction regardless of match
    await supabase.from("sepay_transactions").insert({
      id: tx.id,
      order_id: order?.id || null,
      gateway: tx.gateway,
      transaction_date: tx.transactionDate,
      account_number: tx.accountNumber,
      code: tx.code,
      content: tx.content,
      transfer_type: tx.transferType,
      transfer_amount: tx.transferAmount,
      reference_code: tx.referenceCode,
      raw: tx,
    });

    if (!order) {
      console.log("No matching order found for transaction:", tx.id);
      return new Response(JSON.stringify({ success: true }), {
        status: 200, headers: { "Content-Type": "application/json" },
      });
    }

    // Check amount
    if (tx.transferAmount < order.amount_vnd) {
      await supabase
        .from("orders")
        .update({ status: "underpaid" })
        .eq("id", order.id);

      console.log("Underpaid order:", order.id, "expected:", order.amount_vnd, "got:", tx.transferAmount);
      return new Response(JSON.stringify({ success: true }), {
        status: 200, headers: { "Content-Type": "application/json" },
      });
    }

    // === Payment successful ===

    // Mark order as paid
    await supabase
      .from("orders")
      .update({ status: "paid", paid_at: new Date().toISOString() })
      .eq("id", order.id);

    // Get plan duration
    const durationDays = PLAN_DURATIONS[order.plan_id] || 30;

    // Get or create license
    const { data: existingLicense } = await supabase
      .from("licenses")
      .select("*")
      .eq("user_id", order.user_id)
      .order("expires_at", { ascending: false })
      .limit(1)
      .single();

    // Get plan for max_devices
    const { data: plan } = await supabase
      .from("plans")
      .select("max_devices")
      .eq("id", order.plan_id)
      .single();

    const maxDevices = plan?.max_devices || 1;

    if (existingLicense) {
      // Extend existing license: new_expires = max(current_expires, now) + duration
      const currentExpires = new Date(existingLicense.expires_at);
      const now = new Date();
      const baseDate = currentExpires > now ? currentExpires : now;
      const newExpires = new Date(baseDate.getTime() + durationDays * 24 * 60 * 60 * 1000);

      await supabase
        .from("licenses")
        .update({
          status: "active",
          plan_id: order.plan_id,
          expires_at: newExpires.toISOString(),
          max_devices: maxDevices,
          updated_at: new Date().toISOString(),
        })
        .eq("id", existingLicense.id);

      console.log("License extended:", existingLicense.id, "until:", newExpires.toISOString());
    } else {
      // Create new license
      const newExpires = new Date(Date.now() + durationDays * 24 * 60 * 60 * 1000);

      await supabase.from("licenses").insert({
        user_id: order.user_id,
        status: "active",
        plan_id: order.plan_id,
        expires_at: newExpires.toISOString(),
        max_devices: maxDevices,
      });

      console.log("New license created for user:", order.user_id, "until:", newExpires.toISOString());
    }

    return new Response(JSON.stringify({ success: true }), {
      status: 200, headers: { "Content-Type": "application/json" },
    });

  } catch (error) {
    console.error("Webhook error:", error.message);
    // Always return success to prevent SePay retry storm
    return new Response(JSON.stringify({ success: true }), {
      status: 200, headers: { "Content-Type": "application/json" },
    });
  }
});
