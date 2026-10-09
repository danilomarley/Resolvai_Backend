-- Executar após 001 e 002, com o proprietário do banco, no SQL Editor do Supabase.
begin;

create table public.orders (
    id          uuid primary key default gen_random_uuid(),
    user_id     uuid not null references public.users(id),
    title       varchar(200) not null,
    description text not null default '',
    status      text not null default 'Pending',
    created_at  timestamptz not null default now(),
    updated_at  timestamptz null,
    constraint ck_orders_title check (length(btrim(title)) > 0),
    constraint ck_orders_status check (status in ('Pending', 'InProgress', 'Completed', 'Cancelled'))
);

create index ix_orders_user_created on public.orders (user_id, created_at desc, id desc)
    include (status);

-- Acesso somente pelo backend. Sem políticas de acesso direto pela API do Supabase.
alter table public.orders enable row level security;
revoke all on public.orders from public, anon, authenticated;

commit;
