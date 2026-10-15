import { useState } from 'react';

interface NumberInputProps {
  value: number;
  onChange: (n: number) => void;
  min?: number;
  max?: number;
  step?: number | string;
  scale?: number;
  className?: string;
}

function toText(v: number, scale: number): string {
  return Number.isFinite(v) ? String(v * scale) : '';
}

export default function NumberInput({ value, onChange, min, max, step, scale = 1, className }: NumberInputProps) {
  const [text, setText] = useState(() => toText(value, scale));
  const [prevValue, setPrevValue] = useState(value);

  if (prevValue !== value) {
    setPrevValue(value);
    if (Number(text) !== value * scale) setText(toText(value, scale));
  }

  return (
    <input
      type="number"
      min={min}
      max={max}
      step={step}
      className={className}
      value={text}
      onFocus={(e) => e.currentTarget.select()}
      onChange={(e) => {
        const raw = e.target.value;
        setText(raw);
        onChange(raw === '' ? 0 : Number(raw) / scale);
      }}
    />
  );
}
