<div align="center">

```
██╗  ██╗██████╗  █████╗  █████╗ ██╗  ██╗
██║ ██╔╝██╔══██╗██╔══██╗██╔══██╗██║ ██╔╝
█████╔╝ ██████╔╝███████║███████║█████╔╝ 
██╔═██╗ ██╔══██╗██╔══██║██╔══██║██╔═██╗ 
██║  ██╗██║  ██║██║  ██║██║  ██║██║  ██╗
╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝
```

**Analisador estático de configurações para quem não quer vazar segredo no GitHub.**

[![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![Next.js](https://img.shields.io/badge/Next.js-000000?style=for-the-badge&logo=nextdotjs&logoColor=white)](https://nextjs.org)
[![TypeScript](https://img.shields.io/badge/TypeScript-3178C6?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org)
[![License](https://img.shields.io/badge/Licença-MIT-green?style=for-the-badge)](#%EF%B8%8F-licença)

</div>

---

## 🦅 O que é o Kraak?

<p align="left">
  <img src="/corvo2.png" align="right" width="220" style="margin-left: 20px; border-radius: 15px;">
  <div>
  <p>SAST Lite para arquivos de configuração</p>

  Kraak é um analisador estático de configurações que detecta problemas de segurança em arquivos como `appsettings.json`, `.env` e `docker-compose.yml`. Ele foi projetado para ajudar desenvolvedores a identificar credenciais expostas, configurações inseguras e boas práticas de segurança antes de publicar o código.
  <br><br>
  Sem complicação. Aponta o arquivo, recebe o diagnóstico.
  <br><br>
  </div>
</p>

<br clear="right">

---

## ✨ Funcionalidades

| Recurso | Descrição |
|---|---|
| 🔍 **Análise Estática** | Escaneia arquivos de configuração em busca de credenciais e dados sensíveis |
| 📊 **Sistema de Score** | Pontuação de 0 a 100 que reflete o nível de segurança do projeto |
| 🔴 **Detecção CRITICAL** | Identifica segredos, senhas e tokens expostos diretamente |
| 🟡 **Detecção WARNING** | Aponta configurações inseguras e más práticas |
| 🔵 **Detecção INFO** | Sugestões de melhoria e boas práticas |
| 🌐 **Interface Web** | Dashboard visual para visualizar os resultados de forma clara |
| ⌨️ **CLI** | Rode análises direto no terminal ou em pipelines de CI/CD |

---

## 📁 Arquivos suportados

| Arquivo | Descrição |
|---|---|
| `appsettings.json` | Configuração base do .NET |
| `appsettings.Production.json` | Configuração de produção |
| `.env` | Variáveis de ambiente |
| `.env.local` | Variáveis locais |
| `.env.production` | Variáveis de produção |
| `docker-compose.yml` | Configuração Docker base |
| `docker-compose.prod.yml` | Configuração Docker de produção |

---

## 📉 Sistema de Score

O score começa em **100** e é penalizado por cada problema encontrado:

| Severidade | Penalidade |
|---|---|
| 🔴 `CRITICAL` | -25 pontos por ocorrência |
| 🟡 `WARNING` | -10 pontos por ocorrência |
| 🔵 `INFO` | -2 pontos por ocorrência |

---

## 🛠️ Stack

```
Backend   →  .NET 8 (ASP.NET Core) + C#
Frontend  →  Next.js (App Router) + TypeScript + Tailwind CSS
CLI       →  .NET Console App
```

---

## 🚀 Como rodar

### Pré-requisitos

- [.NET 8+](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)

### Backend

```bash
cd src/Kraak.API
dotnet restore
dotnet run
```

A API sobe em `http://localhost:5000`.

### Frontend

```bash
cd web
npm install
npm run dev
```

O frontend sobe em `http://localhost:3000`.

### CLI

```bash
cd src/Kraak.CLI
dotnet run -- --file caminho/para/appsettings.json
```

---

## 📂 Estrutura do projeto

```
Kraak/
├── src/
│   ├── Kraak.API/        # API REST (.NET)
│   │   ├── Controllers/
│   │   ├── appsettings.json
│   │   └── Program.cs
│   ├── Kraak.CLI/        # Ferramenta de linha de comando
│   │   └── Program.cs
│   └── Kraak.Core/       # Domínio e regras de análise
│       ├── Models/
│       ├── Rules/
│       └── Scanner.cs
└── web/                  # Frontend (Next.js)
```

---

## 🗺️ Roadmap

- [x] Análise de `appsettings.json`
- [x] Análise de `.env` e variantes
- [x] Análise de `docker-compose.yml`
- [x] Sistema de score
- [x] Interface web
- [x] CLI
- [ ] Suporte a `.yaml` genérico
- [ ] Plugin para VS Code
- [ ] Integração com GitHub Actions
- [ ] Exportar relatório em PDF

---

## ⚖️ Licença

Distribuído sob a licença MIT. Veja [LICENSE](LICENSE) para mais detalhes.

---

<div align="center">

Feito com ☕ e muito C# — caw! 🦅

**[⬆ Voltar ao topo](#)**

</div>