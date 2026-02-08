import '@testing-library/jest-dom/vitest';

if (typeof globalThis.ImageData === 'undefined') {
  class ImageDataPolyfill {
    data: Uint8ClampedArray;
    width: number;
    height: number;

    constructor(width: number, height: number);
    constructor(data: ArrayLike<number>, width: number, height: number);
    constructor(
      dataOrWidth: ArrayLike<number> | number,
      widthOrHeight: number,
      maybeHeight?: number
    ) {
      if (typeof dataOrWidth === 'number') {
        this.width = dataOrWidth;
        this.height = widthOrHeight;
        this.data = new Uint8ClampedArray(this.width * this.height * 4);
      } else {
        this.width = widthOrHeight;
        this.height = maybeHeight ?? 0;
        this.data = new Uint8ClampedArray(dataOrWidth);
      }
    }
  }

  Object.defineProperty(globalThis, 'ImageData', {
    value: ImageDataPolyfill,
    writable: true,
    configurable: true,
  });
}

if (typeof HTMLCanvasElement !== 'undefined') {
  const originalGetContext = HTMLCanvasElement.prototype.getContext;

  Object.defineProperty(HTMLCanvasElement.prototype, 'getContext', {
    configurable: true,
    writable: true,
    value: function getContext(this: HTMLCanvasElement, contextId: string) {
      if (contextId !== '2d') {
        return originalGetContext
          ? originalGetContext.call(this, contextId)
          : null;
      }

      const element = this as HTMLCanvasElement & {
        __lastImageData?: ImageData;
        __putImageDataCalls?: number;
      };

      return {
        imageSmoothingEnabled: false,
        putImageData(imageData: ImageData) {
          element.__lastImageData = imageData;
          element.__putImageDataCalls = (element.__putImageDataCalls ?? 0) + 1;
        },
      };
    },
  });
}
