// VinTed Edge Function: verify-license
// Deploy: supabase functions deploy verify-license
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
};

Deno.serve(async (req) => {
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    const supabase = createClient(
      Deno.env.get("SUPABASE_URL")!,
      Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!
    );

    // Verify JWT from Authorization header
    const authHeader = req.headers.get("Authorization");
    if (!authHeader) {
      return new Response(JSON.stringify({ error: "Missing authorization" }), {
        status: 401, headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    const token = authHeader.replace("Bearer ", "");
    const { data: { user }, error: authError } = await supabase.auth.getUser(token);

    if (authError || !user) {
      return new Response(JSON.stringify({ error: "Invalid token" }), {
        status: 401, headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    const { device_id, app_version, device_name } = await req.json();

    // Get active license
    const { data: license } = await supabase
      .from("licenses")
      .select("*")
      .eq("user_id", user.id)
      .eq("status", "active")
      .gte("expires_at", new Date().toISOString())
      .order("expires_at", { ascending: false })
      .limit(1)
      .single();

    if (!license) {
      // Check if there's an expired license
      const { data: expiredLicense } = await supabase
        .from("licenses")
        .select("*")
        .eq("user_id", user.id)
        .order("expires_at", { ascending: false })
        .limit(1)
        .single();

      return new Response(JSON.stringify({
        status: expiredLicense ? "expired" : "inactive",
        plan: expiredLicense?.plan_id || "",
        expires_at: expiredLicense?.expires_at || null,
        server_time: new Date().toISOString(),
        max_devices: 1,
      }), {
        status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" },
      });
    }

    // Upsert device
    if (device_id) {
      const { data: devices } = await supabase
        .from("devices")
        .select("id")
        .eq("user_id", user.id)
        .eq("license_id", license.id);

      const existingDevice = devices?.find((d: any) => false); // placeholder
      const { data: myDevice } = await supabase
        .from("devices")
        .select("id")
        .eq("user_id", user.id)
        .eq("device_hash", device_id)
        .single();

      if (myDevice) {
        // Update last_seen
        await supabase
          .from("devices")
          .update({ last_seen_at: new Date().toISOString(), device_name: device_name || null })
          .eq("id", myDevice.id);
      } else {
        // Check device limit
        const deviceCount = devices?.length || 0;
        if (deviceCount >= license.max_devices) {
          return new Response(JSON.stringify({
            status: "device_limit",
            plan: license.plan_id,
            expires_at: license.expires_at,
            server_time: new Date().toISOString(),
            max_devices: license.max_devices,
            error: "Device limit reached",
          }), {
            status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" },
          });
        }

        // Register new device
        await supabase.from("devices").insert({
          user_id: user.id,
          license_id: license.id,
          device_hash: device_id,
          device_name: device_name || null,
          last_seen_at: new Date().toISOString(),
        });
      }
    }

    return new Response(JSON.stringify({
      status: "active",
      plan: license.plan_id,
      expires_at: license.expires_at,
      server_time: new Date().toISOString(),
      max_devices: license.max_devices,
    }), {
      status: 200, headers: { ...corsHeaders, "Content-Type": "application/json" },
    });

  } catch (error) {
    return new Response(JSON.stringify({ error: error.message }), {
      status: 500, headers: { ...corsHeaders, "Content-Type": "application/json" },
    });
  }
});
