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
        if (!Number.isInteger(dataOrWidth) || !Number.isInteger(widthOrHeight)) {
          throw new TypeError('ImageData width/height must be integers');
        }

        this.width = dataOrWidth;
        this.height = widthOrHeight;
        this.data = new Uint8ClampedArray(this.width * this.height * 4);
      } else {
        if (!(dataOrWidth instanceof Uint8ClampedArray)) {
          throw new TypeError('ImageData data must be Uint8ClampedArray');
        }

        if (!Number.isInteger(widthOrHeight) || !Number.isInteger(maybeHeight)) {
          throw new TypeError('ImageData width/height must be integers');
        }

        this.width = widthOrHeight;
        this.height = maybeHeight;
        const expectedLength = this.width * this.height * 4;

        if (dataOrWidth.length !== expectedLength) {
          throw new TypeError('ImageData data length does not match width/height');
        }

        this.data = dataOrWidth;
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
        __strokeCalls?: number;
      };

      return {
        imageSmoothingEnabled: false,
        strokeStyle: '#000000',
        lineWidth: 1,
        putImageData(imageData: ImageData) {
          element.__lastImageData = imageData;
          element.__putImageDataCalls = (element.__putImageDataCalls ?? 0) + 1;
        },
        beginPath() {},
        moveTo() {},
        lineTo() {},
        stroke() {
          element.__strokeCalls = (element.__strokeCalls ?? 0) + 1;
        },
      };
    },
  });
}
