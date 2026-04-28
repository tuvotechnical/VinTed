-- VinTed License System — Database Schema
-- Chạy trong Supabase SQL Editor

-- Bảng gói license
CREATE TABLE public.plans (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  price_vnd INTEGER NOT NULL,
  duration_days INTEGER NOT NULL,
  max_devices INTEGER NOT NULL DEFAULT 1,
  active BOOLEAN NOT NULL DEFAULT true
);

INSERT INTO public.plans (id, name, price_vnd, duration_days, max_devices) VALUES
  ('daily',   'VinTed 1 ngày',   20000,   1, 1),
  ('monthly', 'VinTed 1 tháng',  199000,  30, 2),
  ('yearly',  'VinTed 1 năm',    1990000, 365, 2);

-- Bảng license
CREATE TABLE public.licenses (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id),
  status TEXT NOT NULL DEFAULT 'inactive',
  plan_id TEXT REFERENCES public.plans(id),
  expires_at TIMESTAMPTZ,
  max_devices INTEGER NOT NULL DEFAULT 1,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Bảng thiết bị
CREATE TABLE public.devices (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id),
  license_id UUID REFERENCES public.licenses(id),
  device_hash TEXT NOT NULL,
  device_name TEXT,
  last_seen_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(user_id, device_hash)
);

-- Bảng đơn hàng
CREATE TABLE public.orders (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id),
  plan_id TEXT NOT NULL REFERENCES public.plans(id),
  order_code TEXT NOT NULL UNIQUE,
  amount_vnd INTEGER NOT NULL,
  status TEXT NOT NULL DEFAULT 'pending',
  expires_at TIMESTAMPTZ NOT NULL DEFAULT now() + interval '30 minutes',
  paid_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Bảng giao dịch SePay (chống trùng lặp bằng SePay transaction ID)
CREATE TABLE public.sepay_transactions (
  id BIGINT PRIMARY KEY,
  order_id UUID REFERENCES public.orders(id),
  gateway TEXT,
  transaction_date TIMESTAMPTZ,
  account_number TEXT,
  code TEXT,
  content TEXT,
  transfer_type TEXT,
  transfer_amount INTEGER,
  reference_code TEXT,
  raw JSONB NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Enable Row Level Security
ALTER TABLE public.plans ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.licenses ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.orders ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.devices ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.sepay_transactions ENABLE ROW LEVEL SECURITY;

-- RLS Policies
-- Plans: ai cũng đọc được
CREATE POLICY "anyone can read plans" ON public.plans
  FOR SELECT USING (true);

-- Licenses: user chỉ đọc license của mình
CREATE POLICY "user reads own licenses" ON public.licenses
  FOR SELECT USING (auth.uid() = user_id);

-- Orders: user chỉ đọc order của mình
CREATE POLICY "user reads own orders" ON public.orders
  FOR SELECT USING (auth.uid() = user_id);

-- Devices: user chỉ đọc device của mình
CREATE POLICY "user reads own devices" ON public.devices
  FOR SELECT USING (auth.uid() = user_id);

-- sepay_transactions: không cho client đọc (chỉ service_role)
-- Không tạo policy = không ai đọc được qua client
