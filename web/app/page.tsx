"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import Link from "next/link";
import Editor from "@monaco-editor/react";

interface Finding {
  ruleId: string;
  title: string;
  description: string;
  filePath: string;
  lineContent: string;
  severity: number;
  suggestion: string;
}

interface ScanResult {
  findings: Finding[];
  score: number;
}

const SEVERITY_LABEL = ["INFO", "WARNING", "CRITICAL"];
const SEVERITY_COLOR = [
  "text-cyan-400 border-cyan-400",
  "text-yellow-400 border-yellow-400",
  "text-red-500 border-red-500",
];
const SEVERITY_BG = [
  "bg-cyan-950",
  "bg-yellow-950",
  "bg-red-950",
];

function CrowImage() {
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

  return <canvas ref={canvasRef} style={{ width: 80, height: 80, imageRendering: "pixelated" }} />;
}

export default function Home() {
  const [fileName, setFileName] = useState("appsettings.json");
  const [content, setContent] = useState("");
  const [result, setResult] = useState<ScanResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [dragging, setDragging] = useState(false);

  const findings = result?.findings ?? null;
  const criticals = findings?.filter(f => f.severity === 2).length ?? 0;
  const warnings = findings?.filter(f => f.severity === 1).length ?? 0;

  const handleFile = useCallback((file: File) => {
    setFileName(file.name);
    const reader = new FileReader();
    reader.onload = e => setContent(e.target?.result as string ?? "");
    reader.readAsText(file);
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setDragging(false);
    const file = e.dataTransfer.files[0];
    if (file) handleFile(file);
  }, [handleFile]);

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setDragging(true);
  };

  const handleDragLeave = () => setDragging(false);

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFile(file);
  };

  const handleScan = async () => {
    if (!content.trim()) return;
    setLoading(true);
    setError("");
    setResult(null);

    try {
      const res = await fetch("http://localhost:5173/api/scan", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ fileName, content }),
      });
      if (!res.ok) throw new Error("Erro na API");
      const data = await res.json();
      setResult(data);
    } catch {
      setError("Não foi possível conectar à API. Verifique se ela está rodando.");
    } finally {
      setLoading(false);
    }
  };

  const handleExport = () => {
    if (!result) return;
    const report = {
      generatedAt: new Date().toISOString(),
      file: fileName,
      score: result.score,
      summary: {
        total: findings!.length,
        critical: criticals,
        warning: warnings,
      },
      findings: result.findings,
    };
    const blob = new Blob([JSON.stringify(report, null, 2)], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `kraak-report-${fileName}-${new Date().toISOString().slice(0, 10)}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const getLanguage = () => {
    if (fileName.endsWith(".json")) return "json";
    if (fileName.endsWith(".yml") || fileName.endsWith(".yaml")) return "yaml";
    return "plaintext";
  };

  return (
    <main className="min-h-screen bg-[#0d0d0d] text-white font-mono p-8">
      <div className="max-w-5xl mx-auto">

        {/* Header */}
        <div className="mb-8 flex flex-col items-center gap-4">
          <div className="flex items-center gap-6">
            <CrowImage />
            <div>
              <pre className="text-green-400 text-xs leading-tight whitespace-pre">{` __  ___ .______          ___           ___       __  ___ 
|  |/  / |   _  \\        /   \\         /   \\     |  |/  / 
|  '  /  |  |_)  |      /  ^  \\       /  ^  \\    |  '  /  
|    <   |      /      /  /_\\  \\     /  /_\\  \\   |    <   
|  .  \\  |  |\\  \\----./  _____  \\   /  _____  \\  |  .  \\  
|__|\\__\\ | _| \`._____/__/     \\__\\ /__/     \\__\\ |__|\\__\\ `}</pre>
              <div className="flex items-center gap-4 mt-1">
                <p className="text-zinc-500 text-sm">Security Analyzer · v0.1.0</p>
                <Link href="/docs" className="text-zinc-500 text-sm hover:text-green-400 transition-colors">
                  Documentação →
                </Link>
              </div>
            </div>
          </div>
        </div>

        {/* Drop Zone */}
        <div
          onDrop={handleDrop}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          className={`mb-4 border-2 border-dashed rounded-lg p-6 text-center transition-colors cursor-pointer ${
            dragging ? "border-green-400 bg-green-950/20" : "border-zinc-700 hover:border-zinc-500"
          }`}
          onClick={() => document.getElementById("fileInput")?.click()}
        >
          <input
            id="fileInput"
            type="file"
            className="hidden"
            accept=".json,.env,.env.local,.env.production,.yml,.yaml"
            onChange={handleFileInput}
          />
          <p className="text-zinc-400 text-sm">
            {dragging ? "📂 Solte o arquivo aqui!" : "📂 Arraste um arquivo aqui ou clique para selecionar"}
          </p>
          <p className="text-zinc-600 text-xs mt-1">
            appsettings.json · .env · .env.local · .env.production · docker-compose.yml
          </p>
          {fileName && content && (
            <p className="text-green-400 text-xs mt-2">✅ {fileName} carregado</p>
          )}
        </div>

        {/* File name select */}
        <div className="mb-3 flex items-center gap-3">
          <label className="text-zinc-400 text-sm">Arquivo:</label>
          <select
            className="bg-zinc-900 border border-zinc-700 text-white text-sm rounded px-3 py-1"
            value={fileName}
            onChange={e => setFileName(e.target.value)}
          >
            <option value="appsettings.json">appsettings.json</option>
            <option value="appsettings.Production.json">appsettings.Production.json</option>
            <option value=".env">.env</option>
            <option value=".env.local">.env.local</option>
            <option value=".env.production">.env.production</option>
            <option value="docker-compose.yml">docker-compose.yml</option>
            <option value="docker-compose.prod.yml">docker-compose.prod.yml</option>
          </select>
        </div>

        {/* Editor */}
        <div className="rounded-lg overflow-hidden border border-zinc-800 mb-4">
          <Editor
            height="320px"
            language={getLanguage()}
            theme="vs-dark"
            value={content}
            onChange={v => setContent(v ?? "")}
            options={{
              fontSize: 13,
              minimap: { enabled: false },
              scrollBeyondLastLine: false,
              lineNumbers: "on",
            }}
          />
        </div>

        {/* Buttons */}
        <div className="flex gap-3 mb-6">
          <button
            onClick={handleScan}
            disabled={loading || !content.trim()}
            className="flex-1 py-3 bg-green-500 hover:bg-green-400 disabled:bg-zinc-700 disabled:text-zinc-500 text-black font-bold rounded-lg transition-colors"
          >
            {loading ? "Analisando..." : "Analisar com Kraak"}
          </button>
          {result && (
            <button
              onClick={handleExport}
              className="py-3 px-6 bg-zinc-800 hover:bg-zinc-700 text-white font-bold rounded-lg transition-colors border border-zinc-600"
            >
              📥 Exportar JSON
            </button>
          )}
        </div>

        {/* Error */}
        {error && (
          <div className="bg-red-950 border border-red-500 text-red-400 rounded-lg p-4 mb-4 text-sm">
            {error}
          </div>
        )}

        {/* Results */}
        {result !== null && (
          <div>
            <div className="mb-4 bg-zinc-900 border border-zinc-800 rounded-lg p-6 flex items-center justify-between">
              <div>
                <div className="text-zinc-400 text-sm mb-1">Score de Segurança</div>
                <div className={`text-4xl font-bold ${
                  result.score >= 80 ? "text-green-400" :
                  result.score >= 50 ? "text-yellow-400" : "text-red-500"
                }`}>
                  {result.score}<span className="text-zinc-600 text-2xl">/100</span>
                </div>
              </div>
              <div className="text-6xl">
                {result.score >= 80 ? "🛡️" : result.score >= 50 ? "⚠️" : "💀"}
              </div>
            </div>

            <div className="flex gap-4 mb-4">
              <div className="flex-1 bg-zinc-900 border border-zinc-800 rounded-lg p-4 text-center">
                <div className="text-2xl font-bold text-white">{findings!.length}</div>
                <div className="text-zinc-500 text-xs mt-1">Total</div>
              </div>
              <div className="flex-1 bg-red-950 border border-red-800 rounded-lg p-4 text-center">
                <div className="text-2xl font-bold text-red-400">{criticals}</div>
                <div className="text-zinc-500 text-xs mt-1">Críticos</div>
              </div>
              <div className="flex-1 bg-yellow-950 border border-yellow-800 rounded-lg p-4 text-center">
                <div className="text-2xl font-bold text-yellow-400">{warnings}</div>
                <div className="text-zinc-500 text-xs mt-1">Warnings</div>
              </div>
            </div>

            {findings!.length === 0 ? (
              <div className="bg-green-950 border border-green-700 text-green-400 rounded-lg p-6 text-center">
                ✅ Nenhum problema encontrado!
              </div>
            ) : (
              <div className="flex flex-col gap-3">
                {findings!.map((f, i) => (
                  <div key={i} className={`rounded-lg border p-4 ${SEVERITY_BG[f.severity]} ${SEVERITY_COLOR[f.severity].split(" ")[1]}`}>
                    <div className="flex items-center gap-2 mb-2">
                      <span className={`text-xs font-bold border px-2 py-0.5 rounded ${SEVERITY_COLOR[f.severity]}`}>
                        {SEVERITY_LABEL[f.severity]}
                      </span>
                      <span className="text-zinc-400 text-xs">{f.ruleId}</span>
                      <span className="font-bold text-sm text-white">{f.title}</span>
                    </div>
                    <p className="text-zinc-300 text-sm mb-2">{f.description}</p>
                    <code className="text-xs bg-black/40 rounded px-2 py-1 block text-zinc-400 truncate mb-2">
                      🔎 {f.lineContent}
                    </code>
                    {f.suggestion && (
                      <div className="text-xs bg-black/30 rounded px-3 py-2 text-green-400 border border-green-900">
                        💡 {f.suggestion}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </main>
  );
}