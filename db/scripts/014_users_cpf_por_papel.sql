begin;

alter table public.users drop constraint uq_users_cpf;
alter table public.users add constraint uq_users_cpf_role unique (cpf, role);

commit;