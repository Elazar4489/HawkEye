import React from 'react';
import { AlertTriangle, X, ShieldAlert } from 'lucide-react';

interface Props {
  message: string;
  onClose: () => void;
}

export const ToastAlert: React.FC<Props> = ({ message, onClose }) => {
  return (
    <div className="fixed bottom-6 left-6 z-50 flex max-w-md items-center space-x-3 space-x-reverse rounded-2xl border border-rose-500/50 bg-[#0B0F19]/95 p-4 shadow-2xl shadow-rose-500/20 text-gray-100 backdrop-blur-xl animate-in slide-in-from-bottom duration-300">
      <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl bg-rose-500/20 text-rose-400 border border-rose-500/40">
        <AlertTriangle size={22} className="animate-bounce" />
      </div>

      <div className="flex-1">
        <div className="flex items-center space-x-2 space-x-reverse">
          <ShieldAlert size={14} className="text-rose-400" />
          <h4 className="text-xs font-bold uppercase tracking-wider text-rose-400">התראת חציית אזור בזמן אמת</h4>
        </div>
        <p className="mt-1 text-xs text-gray-200 font-semibold leading-relaxed">{message}</p>
      </div>

      <button
        onClick={onClose}
        className="rounded-lg p-1 text-gray-400 hover:bg-gray-800 hover:text-white transition"
      >
        <X size={16} />
      </button>
    </div>
  );
};
