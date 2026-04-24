"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import Link from "next/link";
import Editor from "@monaco-editor/react";

interface DriftFinding {
  ruleId: string;
  title: string;
  description: string;
  lineContent: string;
  severity: number;
  suggestion: string;
}

const SEVERITY_LABEL = ["INFO", "WARNING", "CRITICAL"];
const SEVERITY_COLOR = [
  "text-cyan-400 border-cyan-400",
  "text-yellow-400 border-yellow-400",
  "text-red-500 border-red-500",
];
const SEVERITY_BG = ["bg-cyan-950", "bg-yellow-950", "bg-red-950"];

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
  return <canvas ref={canvasRef} style={{ width: 48, height: 48, imageRendering: "pixelated" }} />;
}

export default function DriftPage() {
  const [oldContent, setOldContent] = useState("");
  const [newContent, setNewContent] = useState("");
  const [fileName, setFileName] = useState("appsettings.json");
  const [findings, setFindings] = useState<DriftFinding[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const getLanguage = () => {
    if (fileName.endsWith(".json")) return "json";
    if (fileName.endsWith(".yml") || fileName.endsWith(".yaml")) return "yaml";
    return "plaintext";
  };

  const handleFileLoad = (setter: (c: string) => void, nameUpdate = false) => (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (nameUpdate) setFileName(file.name);
    const reader = new FileReader();
    reader.onload = ev => setter(ev.target?.result as string ?? "");
    reader.readAsText(file);
    e.target.value = "";
  };

  const handleCompare = async () => {
    if (!oldContent.trim() || !newContent.trim()) return;
    setLoading(true);
    setError("");
    setFindings(null);

    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL ?? "https://kraak-production.up.railway.app"}/api/drift`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ fileName, oldContent, newContent }),
        }
      );
      if (!res.ok) throw new Error("Erro na API");
      const data = await res.json();
      setFindings(data);
    } catch {
      setError("Não foi possível conectar à API.");
    } finally {
      setLoading(false);
    }
  };

  const added = findings?.filter(f => f.ruleId === "KRK015").length ?? 0;
  const removed = findings?.filter(f => f.ruleId === "KRK016").length ?? 0;
  const changed = findings?.filter(f => f.ruleId === "KRK017").length ?? 0;

  return (
    <main className="min-h-screen bg-[#0d0d0d] text-white font-mono p-4 sm:p-8">
      <div className="max-w-5xl mx-auto">

        {/* Header */}
        <div className="mb-8 flex items-center gap-4">
          <CrowImage />
          <div>
            <div className="flex items-center gap-3">
              <h1 className="text-green-400 text-xl font-bold">Drift Detector</h1>
              <span className="text-zinc-600 text-xs border border-zinc-700 px-2 py-0.5 rounded">BETA</span>
            </div>
            <div className="flex gap-4 mt-1">
              <Link href="/" className="text-zinc-500 text-xs hover:text-green-400 transition-colors">← Analisador</Link>
              <Link href="/docs" className="text-zinc-500 text-xs hover:text-green-400 transition-colors">Documentação →</Link>
            </div>
          </div>
        </div>

        {/* Tipo de arquivo */}
        <div className="flex items-center gap-2 mb-4">
          <span className="text-zinc-500 text-xs">Tipo de arquivo:</span>
          <select
            value={fileName}
            onChange={e => setFileName(e.target.value)}
            className="bg-zinc-800 border border-zinc-700 text-white text-xs rounded px-2 py-1"
          >
            {["appsettings.json", "appsettings.Production.json", ".env", ".env.local", ".env.production", "docker-compose.yml"].map(f => (
              <option key={f} value={f}>{f}</option>
            ))}
          </select>
        </div>

        {/* Editores lado a lado */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
          {/* Versão antiga */}
          <div className="rounded-lg overflow-hidden border border-zinc-800">
            <div className="bg-zinc-900 px-3 py-2 border-b border-zinc-800 flex items-center justify-between">
              <span className="text-zinc-400 text-xs">📄 Versão Antiga</span>
              <button
                onClick={() => document.getElementById("oldFile")?.click()}
                className="text-zinc-500 text-xs hover:text-green-400 transition-colors cursor-pointer"
              >
                Carregar arquivo
              </button>
              <input id="oldFile" type="file" className="hidden" onChange={handleFileLoad(setOldContent)} />
            </div>
            <Editor
              height="300px"
              language={getLanguage()}
              theme="vs-dark"
              value={oldContent}
              onChange={v => setOldContent(v ?? "")}
              options={{ fontSize: 12, minimap: { enabled: false }, lineNumbers: "on", wordWrap: "on", scrollBeyondLastLine: false }}
            />
          </div>

          {/* Versão nova */}
          <div className="rounded-lg overflow-hidden border border-zinc-800">
            <div className="bg-zinc-900 px-3 py-2 border-b border-zinc-800 flex items-center justify-between">
              <span className="text-zinc-400 text-xs">📄 Versão Nova</span>
              <button
                onClick={() => document.getElementById("newFile")?.click()}
                className="text-zinc-500 text-xs hover:text-green-400 transition-colors cursor-pointer"
              >
                Carregar arquivo
              </button>
              <input id="newFile" type="file" className="hidden" onChange={handleFileLoad(setNewContent, true)} />
            </div>
            <Editor
              height="300px"
              language={getLanguage()}
              theme="vs-dark"
              value={newContent}
              onChange={v => setNewContent(v ?? "")}
              options={{ fontSize: 12, minimap: { enabled: false }, lineNumbers: "on", wordWrap: "on", scrollBeyondLastLine: false }}
            />
          </div>
        </div>

        {/* Botão */}
        <button
          onClick={handleCompare}
          disabled={loading || !oldContent.trim() || !newContent.trim()}
          className="w-full py-3 bg-green-500 hover:bg-green-400 disabled:bg-zinc-700 disabled:text-zinc-500 text-black font-bold rounded-lg transition-colors mb-6"
        >
          {loading ? "Comparando..." : "🔀 Comparar com Kraak"}
        </button>

        {/* Error */}
        {error && (
          <div className="bg-red-950 border border-red-500 text-red-400 rounded-lg p-4 mb-4 text-xs">
            {error}
          </div>
        )}

        {/* Results */}
        {findings !== null && (
          <div>
            <div className="grid grid-cols-3 gap-3 mb-4">
              <div className="bg-green-950 border border-green-800 rounded-lg p-3 text-center">
                <div className="text-xl font-bold text-green-400">{added}</div>
                <div className="text-zinc-500 text-[10px] mt-1">Adicionadas</div>
              </div>
              <div className="bg-red-950 border border-red-800 rounded-lg p-3 text-center">
                <div className="text-xl font-bold text-red-400">{removed}</div>
                <div className="text-zinc-500 text-[10px] mt-1">Removidas</div>
              </div>
              <div className="bg-yellow-950 border border-yellow-800 rounded-lg p-3 text-center">
                <div className="text-xl font-bold text-yellow-400">{changed}</div>
                <div className="text-zinc-500 text-[10px] mt-1">Alteradas</div>
              </div>
            </div>

            {findings.length === 0 ? (
              <div className="bg-green-950 border border-green-700 text-green-400 rounded-lg p-6 text-center">
                ✅ Nenhuma diferença encontrada!
              </div>
            ) : (
              <div className="flex flex-col gap-3">
                {findings.map((f, i) => (
                  <div key={i} className={`rounded-lg border p-4 ${SEVERITY_BG[f.severity]} ${SEVERITY_COLOR[f.severity].split(" ")[1]}`}>
                    <div className="flex flex-wrap items-center gap-2 mb-2">
                      <span className={`text-[10px] font-bold border px-2 py-0.5 rounded ${SEVERITY_COLOR[f.severity]}`}>
                        {SEVERITY_LABEL[f.severity]}
                      </span>
                      <span className="text-zinc-400 text-[10px]">{f.ruleId}</span>
                      <span className="font-bold text-xs text-white">{f.title}</span>
                    </div>
                    <p className="text-zinc-300 text-xs mb-2">{f.description}</p>
                    <code className="text-[10px] bg-black/40 rounded px-2 py-1 block text-zinc-400 truncate mb-2">
                      🔎 {f.lineContent}
                    </code>
                    {f.suggestion && (
                      <div className="text-[10px] bg-black/30 rounded px-3 py-2 text-green-400 border border-green-900">
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
