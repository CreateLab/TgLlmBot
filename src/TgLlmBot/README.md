## Postgres docker

```bash
docker run -d -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:18
```

Connection string

```text
Host=localhost;Port=5432;Database=tgbot;Username=postgres;Password=postgres;
```
