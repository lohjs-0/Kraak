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

interface FileEntry {
  fileName: string;
  content: string;
}

const SEVERITY_LABEL = ["INFO", "WARNING", "CRITICAL"];
const SEVERITY_COLOR = [
  "text-cyan-400 border-cyan-400",
  "text-yellow-400 border-yellow-400",
  "text-red-500 border-red-500",
];
const SEVERITY_BG = ["bg-cyan-950", "bg-yellow-950", "bg-red-950"];

const ACCEPTED_FILES = [
  "appsettings.json",
  "appsettings.Production.json",
  ".env",
  ".env.local",
  ".env.production",
  ".env.development",
  "docker-compose.yml",
  "docker-compose.prod.yml",
  ".gitignore",
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

  return (
    <canvas
      ref={canvasRef}
      className="w-12 h-12 sm:w-16 sm:h-16 md:w-20 md:h-20"
      style={{ imageRendering: "pixelated" }}
    />
  );
}

export default function Home() {
  const [files, setFiles] = useState<FileEntry[]>([]);
  const [activeFile, setActiveFile] = useState<string | null>(null);
  const [manualFileName, setManualFileName] = useState(".env");
  const [manualContent, setManualContent] = useState("");
  const [result, setResult] = useState<ScanResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [dragging, setDragging] = useState(false);
  const [editorHeight, setEditorHeight] = useState(280);

  useEffect(() => {
    const update = () => setEditorHeight(window.innerWidth < 640 ? 200 : 320);
    update();
    window.addEventListener("resize", update);
    return () => window.removeEventListener("resize", update);
  }, []);

  const findings = result?.findings ?? null;
  const criticals = findings?.filter(f => f.severity === 2).length ?? 0;
  const warnings = findings?.filter(f => f.severity === 1).length ?? 0;

  const addFile = useCallback((file: File) => {
    const reader = new FileReader();
    reader.onload = e => {
      const content = e.target?.result as string ?? "";
      setFiles(prev => {
        const exists = prev.findIndex(f => f.fileName === file.name);
        if (exists >= 0) {
          const updated = [...prev];
          updated[exists] = { fileName: file.name, content };
          return updated;
        }
        return [...prev, { fileName: file.name, content }];
      });
      setActiveFile(file.name);
    };
    reader.readAsText(file);
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setDragging(false);
    Array.from(e.dataTransfer.files).forEach(addFile);
  }, [addFile]);

  const handleDragOver = (e: React.DragEvent) => { e.preventDefault(); setDragging(true); };
  const handleDragLeave = () => setDragging(false);

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    Array.from(e.target.files ?? []).forEach(addFile);
    e.target.value = "";
  };

  const handleRemoveFile = (fileName: string) => {
    setFiles(prev => prev.filter(f => f.fileName !== fileName));
    setActiveFile(prev => prev === fileName ? null : prev);
  };

  const activeEntry = files.find(f => f.fileName === activeFile);

  const updateActiveContent = (content: string) => {
    setFiles(prev => prev.map(f => f.fileName === activeFile ? { ...f, content } : f));
  };

  const getLanguage = (fileName: string) => {
    if (fileName.endsWith(".json")) return "json";
    if (fileName.endsWith(".yml") || fileName.endsWith(".yaml")) return "yaml";
    return "plaintext";
  };

  const allFilesToScan = (): FileEntry[] => {
    const loaded = files;
    if (manualContent.trim() && !files.find(f => f.fileName === manualFileName)) {
      return [...loaded, { fileName: manualFileName, content: manualContent }];
    }
    return loaded;
  };

  const canScan = allFilesToScan().length > 0;

  const handleScan = async () => {
    const toScan = allFilesToScan();
    if (toScan.length === 0) return;
    setLoading(true);
    setError("");
    setResult(null);

    try {
      const res = await fetch(
        `${process.env.NEXT_PUBLIC_API_URL ?? "https://kraak-production.up.railway.app"}/api/scan`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(toScan),
        }
      );
      if (!res.ok) throw new Error("Erro na API");
      const data = await res.json();
      setResult(data);
    } catch {
      setError("Não foi possível conectar à API. Verifique se ela está rodando.");
    } finally {
      setLoading(false);
    }
  };

  const handleClear = () => {
    setFiles([]);
    setActiveFile(null);
    setManualContent("");
    setManualFileName(".env");
    setResult(null);
    setError("");
  };

  const handleExport = () => {
    if (!result) return;
    const report = {
      generatedAt: new Date().toISOString(),
      files: allFilesToScan().map(f => f.fileName),
      score: result.score,
      summary: { total: findings!.length, critical: criticals, warning: warnings },
      findings: result.findings,
    };
    const blob = new Blob([JSON.stringify(report, null, 2)], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `kraak-report-${new Date().toISOString().slice(0, 10)}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <main className="min-h-screen bg-[#0d0d0d] text-white font-mono">
      <div className="max-w-5xl mx-auto px-4 py-6 sm:px-6 sm:py-8">

        {/* Header */}
        <div className="mb-6 sm:mb-8 flex flex-col items-center gap-3 sm:gap-4">
          <div className="flex items-center gap-3 sm:gap-6 w-full justify-center">
            <CrowImage />
            <div className="min-w-0">
              <pre className="hidden sm:block text-green-400 text-[10px] md:text-xs leading-tight whitespace-pre overflow-x-auto">{` __  ___ .______          ___           ___       __  ___ 
|  |/  / |   _  \\        /   \\         /   \\     |  |/  / 
|  '  /  |  |_)  |      /  ^  \\       /  ^  \\    |  '  /  
|    <   |      /      /  /_\\  \\     /  /_\\  \\   |    <   
|  .  \\  |  |\\  \\----./  _____  \\   /  _____  \\  |  .  \\  
|__|\\__\\ | _| \`._____/__/     \\__\\ /__/     \\__\\ |__|\\__\\ `}</pre>
              <div className="sm:hidden text-green-400 text-2xl font-bold tracking-widest">KRAAK</div>
              <div className="flex flex-wrap items-center gap-2 sm:gap-4 mt-1">
                <p className="text-zinc-500 text-xs sm:text-sm">Security Analyzer · v0.1.0</p>
                <Link href="/docs" className="text-zinc-500 text-xs sm:text-sm hover:text-green-400 transition-colors cursor-pointer">
                  Documentação →
                </Link>

                <Link href="/drift" className="text-zinc-500 text-xs sm:text-sm hover:text-green-400 transition-colors cursor-pointer">
                  Drift →
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
          className={`mb-4 border-2 border-dashed rounded-lg p-4 sm:p-6 text-center transition-colors cursor-pointer ${
            dragging ? "border-green-400 bg-green-950/20" : "border-zinc-700 hover:border-zinc-500"
          }`}
          onClick={() => document.getElementById("fileInput")?.click()}
        >
          <input
            id="fileInput"
            type="file"
            className="hidden"
            multiple
            accept=".json,.env,.env.local,.env.production,.env.development,.yml,.yaml,.gitignore"
            onChange={handleFileInput}
          />
          <p className="text-zinc-400 text-sm">
            {dragging ? "📂 Solte os arquivos aqui!" : "📂 Toque para selecionar arquivos"}
          </p>
          <p className="text-zinc-600 text-xs mt-1 hidden sm:block">
            Você pode carregar múltiplos arquivos — ex: .env + .gitignore juntos
          </p>
        </div>

        {/* Arquivos carregados */}
        {files.length > 0 && (
          <div className="mb-3 flex flex-wrap gap-2">
            {files.map(f => (
              <button
                key={f.fileName}
                onClick={() => setActiveFile(f.fileName)}
                className={`flex items-center gap-1 text-xs px-3 py-1 rounded-full border transition-colors cursor-pointer ${
                  activeFile === f.fileName
                    ? "border-green-400 text-green-400 bg-green-950/30"
                    : "border-zinc-700 text-zinc-400 hover:border-zinc-500"
                }`}
              >
                {f.fileName}
                <span
                  onClick={e => { e.stopPropagation(); handleRemoveFile(f.fileName); }}
                  className="ml-1 text-zinc-600 hover:text-red-400 transition-colors cursor-pointer"
                >
                  ×
                </span>
              </button>
            ))}
          </div>
        )}

        {/* Editor — arquivo carregado */}
        {activeEntry && (
          <div className="rounded-lg overflow-hidden border border-zinc-800 mb-4">
            <div className="bg-zinc-900 px-3 py-1.5 text-xs text-zinc-500 border-b border-zinc-800">
              {activeEntry.fileName}
            </div>
            <Editor
              height={`${editorHeight}px`}
              language={getLanguage(activeEntry.fileName)}
              theme="vs-dark"
              value={activeEntry.content}
              onChange={v => updateActiveContent(v ?? "")}
              options={{
                fontSize: 12,
                minimap: { enabled: false },
                scrollBeyondLastLine: false,
                lineNumbers: "on",
                wordWrap: "on",
              }}
            />
          </div>
        )}

        {/* Editor manual */}
        {!activeEntry && (
          <div className="rounded-lg overflow-hidden border border-zinc-800 mb-4">
            <div className="bg-zinc-900 px-3 py-2 border-b border-zinc-800 flex items-center gap-2">
              <span className="text-zinc-500 text-xs shrink-0">Tipo de arquivo:</span>
              <select
                value={manualFileName}
                onChange={e => setManualFileName(e.target.value)}
                className="bg-zinc-800 border border-zinc-700 text-white text-xs rounded px-2 py-1 cursor-pointer hover:border-zinc-500 transition-colors"
              >
                {ACCEPTED_FILES.map(f => (
                  <option key={f} value={f}>{f}</option>
                ))}
              </select>
            </div>
            <Editor
              height={`${editorHeight}px`}
              language={getLanguage(manualFileName)}
              theme="vs-dark"
              value={manualContent}
              onChange={v => setManualContent(v ?? "")}
              options={{
                fontSize: 12,
                minimap: { enabled: false },
                scrollBeyondLastLine: false,
                lineNumbers: "on",
                wordWrap: "on",
              }}
            />
          </div>
        )}

        {/* Buttons */}
        <div className="flex gap-3 mb-6">
          <button
            onClick={handleScan}
            disabled={loading || !canScan}
            className="flex-1 py-3 bg-green-500 hover:bg-green-400 active:bg-green-600 disabled:bg-zinc-700 disabled:text-zinc-500 disabled:cursor-not-allowed text-black font-bold rounded-lg transition-colors text-sm sm:text-base cursor-pointer"
          >
            {loading ? "Analisando..." : `Analisar com Kraak${allFilesToScan().length > 1 ? ` (${allFilesToScan().length} arquivos)` : ""}`}
          </button>
          <button
            onClick={handleClear}
            disabled={!canScan && !manualContent}
            className="py-3 px-5 bg-zinc-800 hover:bg-zinc-700 disabled:bg-zinc-900 disabled:text-zinc-700 disabled:cursor-not-allowed text-white font-bold rounded-lg transition-colors border border-zinc-600 text-sm cursor-pointer"
          >
            🗑️
          </button>
          {result && (
            <button
              onClick={handleExport}
              className="py-3 px-5 sm:px-6 bg-zinc-800 hover:bg-zinc-700 active:bg-zinc-600 text-white font-bold rounded-lg transition-colors border border-zinc-600 text-sm sm:text-base cursor-pointer"
            >
              📥
            </button>
          )}
        </div>

        {/* Error */}
        {error && (
          <div className="bg-red-950 border border-red-500 text-red-400 rounded-lg p-4 mb-4 text-xs sm:text-sm">
            {error}
          </div>
        )}

        {/* Results */}
        {result !== null && (
          <div>
            <div className="mb-4 bg-zinc-900 border border-zinc-800 rounded-lg p-4 sm:p-6 flex items-center justify-between">
              <div>
                <div className="text-zinc-400 text-xs sm:text-sm mb-1">Score de Segurança</div>
                <div className={`text-3xl sm:text-4xl font-bold ${
                  result.score >= 80 ? "text-green-400" :
                  result.score >= 50 ? "text-yellow-400" : "text-red-500"
                }`}>
                  {result.score}<span className="text-zinc-600 text-xl sm:text-2xl">/100</span>
                </div>
              </div>
              <div className="text-4xl sm:text-6xl">
                {result.score >= 80 ? "🛡️" : result.score >= 50 ? "⚠️" : "💀"}
              </div>
            </div>

            <div className="grid grid-cols-3 gap-2 sm:gap-4 mb-4">
              <div className="bg-zinc-900 border border-zinc-800 rounded-lg p-3 sm:p-4 text-center">
                <div className="text-xl sm:text-2xl font-bold text-white">{findings!.length}</div>
                <div className="text-zinc-500 text-[10px] sm:text-xs mt-1">Total</div>
              </div>
              <div className="bg-red-950 border border-red-800 rounded-lg p-3 sm:p-4 text-center">
                <div className="text-xl sm:text-2xl font-bold text-red-400">{criticals}</div>
                <div className="text-zinc-500 text-[10px] sm:text-xs mt-1">Críticos</div>
              </div>
              <div className="bg-yellow-950 border border-yellow-800 rounded-lg p-3 sm:p-4 text-center">
                <div className="text-xl sm:text-2xl font-bold text-yellow-400">{warnings}</div>
                <div className="text-zinc-500 text-[10px] sm:text-xs mt-1">Warnings</div>
              </div>
            </div>

            {findings!.length === 0 ? (
              <div className="bg-green-950 border border-green-700 text-green-400 rounded-lg p-4 sm:p-6 text-center text-sm">
                ✅ Nenhum problema encontrado!
              </div>
            ) : (
              <div className="flex flex-col gap-3">
                {findings!.map((f, i) => (
                  <div key={i} className={`rounded-lg border p-3 sm:p-4 ${SEVERITY_BG[f.severity]} ${SEVERITY_COLOR[f.severity].split(" ")[1]}`}>
                    <div className="flex flex-wrap items-center gap-2 mb-2">
                      <span className={`text-[10px] sm:text-xs font-bold border px-2 py-0.5 rounded shrink-0 ${SEVERITY_COLOR[f.severity]}`}>
                        {SEVERITY_LABEL[f.severity]}
                      </span>
                      <span className="text-zinc-400 text-[10px] sm:text-xs shrink-0">{f.ruleId}</span>
                      <span className="font-bold text-xs sm:text-sm text-white break-words">{f.title}</span>
                    </div>
                    <p className="text-zinc-300 text-xs sm:text-sm mb-2">{f.description}</p>
                    <code className="text-[10px] sm:text-xs bg-black/40 rounded px-2 py-1 block text-zinc-400 truncate mb-2">
                      🔎 {f.lineContent}
                    </code>
                    {f.suggestion && (
                      <div className="text-[10px] sm:text-xs bg-black/30 rounded px-3 py-2 text-green-400 border border-green-900 break-words">
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
