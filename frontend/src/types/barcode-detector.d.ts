// Ambient declaration for the Barcode Detection API (Chrome/Edge/Android) — not yet part of
// TypeScript's bundled DOM lib. Feature-detected at runtime via `'BarcodeDetector' in window`;
// browsers without it (Safari, Firefox) fall back to the jsQR decode path instead.
interface DetectedBarcode {
  readonly rawValue: string;
}

interface BarcodeDetectorOptions {
  formats?: string[];
}

declare class BarcodeDetector {
  constructor(options?: BarcodeDetectorOptions);
  detect(source: CanvasImageSource): Promise<DetectedBarcode[]>;
}
