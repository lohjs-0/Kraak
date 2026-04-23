"use client";

import Link from "next/link";
import { useEffect, useRef } from "react";

function CrowImage({ size = 40 }: { size?: number }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    const img = new window.Image();
    img.src = "/corvo.png";
    img.onload = () => {
      canvas.width = img.width;
      canvas.height = img.height;
      ctx.drawImage(img, 0, 0);
      const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
      const data = imageData.data;
      for (let i = 0; i < data.length; i += 4) {
        const r = data[i], g = data[i + 1], b = data[i + 2];
        if (r < 30 && g < 30 && b < 30) data[i + 3] = 0;
      }
      ctx.putImageData(imageData, 0, 0);
    };
  }, []);

  return <canvas ref={canvasRef} style={{ width: size, height: size, imageRendering: "pixelated" }} />;
}

const rules = [
  {
    id: "KRK001",
    title: "Connection String Exposta",
    severity: "CRITICAL",
    description: "Detecta senhas expostas em connection strings do appsettings.json. Verifica os padrões 'Password=' e 'Pwd='.",
    example: `"ConnectionStrings": {\n  "Default": "Server=localhost;Password=123;" ← PROBLEMA\n}`,
    fix: `"ConnectionStrings": {\n  "Default": "Server=localhost;User Id=app;" ← SEGURO\n}`,
  },
  {
    id: "KRK002",
    title: "AllowedHosts Inseguro",
    severity: "WARNING",
    description: "Detecta quando AllowedHosts está configurado como '*', permitindo requisições de qualquer host.",
    example: `"AllowedHosts": "*" ← PROBLEMA`,
    fix: `"AllowedHosts": "meusite.com" ← SEGURO`,
  },
  {
    id: "KRK003",
    title: "Chave de API Hardcoded",
    severity: "CRITICAL",
    description: "Detecta tokens e chaves de API conhecidas hardcodadas em arquivos JSON. Suporta AWS, Stripe, GitHub, Google, Slack, OpenAI, Twilio e SendGrid.",
    example: `"Stripe": {\n  "SecretKey": "sk_live_51ABC..." ← PROBLEMA\n}`,
    fix: `"Stripe": {\n  "SecretKey": "" ← use variável de ambiente\n}`,
  },
  {
    id: "KRK004",
    title: "Arquivo .env sem proteção",
    severity: "WARNING",
    description: "Verifica se existe um .gitignore na pasta do arquivo analisado e se ele protege o .env de ser commitado acidentalmente.",
    example: `# .gitignore vazio ou inexistente ← PROBLEMA`,
    fix: `# .gitignore\n.env\n.env.local\n.env.production ← SEGURO`,
  },
  {
    id: "KRK005",
    title: "HTTPS Desabilitado",
    severity: "CRITICAL",
    description: "Detecta quando o redirecionamento HTTPS está explicitamente desabilitado, expondo a aplicação a ataques Man-in-the-Middle.",
    example: `"HttpsRedirection": {\n  "Enabled": false ← PROBLEMA\n}`,
    fix: `"HttpsRedirection": {\n  "Enabled": true ← SEGURO\n}`,
  },
  {
    id: "KRK006",
    title: "Debug Ativo em Produção",
    severity: "WARNING / CRITICAL",
    description: "Detecta nível de log 'Debug' ou 'Trace' que pode expor dados sensíveis, e ambiente configurado como 'Development' em produção.",
    example: `"Logging": { "LogLevel": { "Default": "Debug" } } ← PROBLEMA\n"Environment": "Development" ← PROBLEMA`,
    fix: `"Logging": { "LogLevel": { "Default": "Warning" } } ← SEGURO`,
  },
  {
    id: "KRK007",
    title: "Secret Exposto em .env",
    severity: "CRITICAL",
    description: "Detecta senhas, tokens e chaves de API expostas dentro de arquivos .env. Suporta os mesmos padrões do KRK003 além de variáveis genéricas como PASSWORD=, API_KEY= e TOKEN=.",
    example: `API_KEY=abc123supersecreta ← PROBLEMA\nPASSWORD=minhasenha ← PROBLEMA`,
    fix: `# Use variáveis de ambiente do servidor\n# ou AWS Secrets Manager / Azure Key Vault`,
  },
];

const SEVERITY_COLOR: Record<string, string> = {
  "CRITICAL": "text-red-500 border-red-500 bg-red-950",
  "WARNING": "text-yellow-400 border-yellow-400 bg-yellow-950",
  "WARNING / CRITICAL": "text-orange-400 border-orange-400 bg-orange-950",
};

export default function Docs() {
  return (
    <main className="min-h-screen bg-[#0d0d0d] text-white font-mono p-8">
      <div className="max-w-4xl mx-auto">

        {/* Header */}
        <div className="mb-10">
          <Link href="/" className="text-zinc-500 text-sm hover:text-green-400 transition-colors">
            ← Voltar para o Kraak
          </Link>
          <div className="flex items-center gap-3 mt-4">
            <CrowImage size={48} />
            <h1 className="text-3xl font-bold text-green-400">Documentação</h1>
          </div>
          <p className="text-zinc-400 mt-2">Todas as regras de segurança do Kraak e como corrigi-las.</p>
        </div>

        {/* O que é o Kraak */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6 mb-6">
          <div className="flex items-center gap-2 mb-3">
            <CrowImage size={24} />
            <h2 className="text-lg font-bold text-white">O que é o Kraak?</h2>
          </div>
          <p className="text-zinc-400 text-sm leading-relaxed">
            O Kraak é um analisador estático de configurações (SAST Lite) que detecta problemas de segurança
            em arquivos de configuração como <span className="text-green-400">appsettings.json</span> e <span className="text-green-400">.env</span>.
            Ele foi projetado para ajudar desenvolvedores a identificar credenciais expostas, configurações inseguras
            e boas práticas de segurança antes de publicar o código.
          </p>
        </div>

        {/* Arquivos suportados */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6 mb-6">
          <div className="flex items-center gap-2 mb-3">
            <CrowImage size={24} />
            <h2 className="text-lg font-bold text-white">Arquivos Suportados</h2>
          </div>
          <div className="flex flex-wrap gap-2">
            {["appsettings.json", "appsettings.Production.json", ".env", ".env.local", ".env.production"].map(f => (
              <span key={f} className="bg-zinc-800 text-green-400 text-xs px-3 py-1 rounded border border-zinc-700">
                {f}
              </span>
            ))}
          </div>
        </div>

        {/* Sistema de Score */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6 mb-6">
          <div className="flex items-center gap-2 mb-3">
            <CrowImage size={24} />
            <h2 className="text-lg font-bold text-white">Sistema de Score</h2>
          </div>
          <p className="text-zinc-400 text-sm mb-4">O score começa em 100 e é penalizado por cada problema encontrado:</p>
          <div className="flex flex-col gap-2">
            <div className="flex items-center gap-3">
              <span className="text-red-500 font-bold text-sm w-20">CRITICAL</span>
              <span className="text-zinc-400 text-sm">-25 pontos por ocorrência</span>
            </div>
            <div className="flex items-center gap-3">
              <span className="text-yellow-400 font-bold text-sm w-20">WARNING</span>
              <span className="text-zinc-400 text-sm">-10 pontos por ocorrência</span>
            </div>
            <div className="flex items-center gap-3">
              <span className="text-cyan-400 font-bold text-sm w-20">INFO</span>
              <span className="text-zinc-400 text-sm">-2 pontos por ocorrência</span>
            </div>
          </div>
        </div>

        {/* Regras */}
        <div className="flex items-center gap-2 mb-4">
          <CrowImage size={24} />
          <h2 className="text-lg font-bold text-white">Regras</h2>
        </div>
        <div className="flex flex-col gap-6">
          {rules.map(rule => (
            <div key={rule.id} className="bg-zinc-900 border border-zinc-800 rounded-lg p-6">
              <div className="flex items-center gap-3 mb-3">
                <span className={`text-xs font-bold border px-2 py-0.5 rounded ${SEVERITY_COLOR[rule.severity]}`}>
                  {rule.severity}
                </span>
                <span className="text-zinc-500 text-xs">{rule.id}</span>
                <span className="font-bold text-white">{rule.title}</span>
              </div>
              <p className="text-zinc-400 text-sm mb-4">{rule.description}</p>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <p className="text-red-400 text-xs mb-1">❌ Problema</p>
                  <pre className="bg-black/40 rounded p-3 text-xs text-zinc-400 overflow-auto whitespace-pre-wrap">{rule.example}</pre>
                </div>
                <div>
                  <p className="text-green-400 text-xs mb-1">✅ Solução</p>
                  <pre className="bg-black/40 rounded p-3 text-xs text-zinc-400 overflow-auto whitespace-pre-wrap">{rule.fix}</pre>
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* CLI */}
        <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-6 mt-8">
          <div className="flex items-center gap-2 mb-3">
            <CrowImage size={24} />
            <h2 className="text-lg font-bold text-white">Usando a CLI</h2>
          </div>
          <p className="text-zinc-400 text-sm mb-3">O Kraak também pode ser usado via linha de comando:</p>
          <pre className="bg-black/40 rounded p-3 text-xs text-green-400 overflow-auto">
{`# Analisar um arquivo appsettings.json
dotnet run --project src/Kraak.CLI -- appsettings.json

# Analisar um arquivo .env
dotnet run --project src/Kraak.CLI -- .env`}
          </pre>
        </div>

        {/* Footer */}
        <div className="text-center text-zinc-600 text-xs mt-10">
          Kraak · Security Analyzer · v0.1.0
        </div>

      </div>
    </main>
  );
}