import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

declare global {
  interface Window {
    MercadoPago: new (publicKey: string, options?: { locale: string }) => MercadoPagoInstance;
  }
}

interface MercadoPagoInstance {
  bricks(): BricksBuilder;
}

interface BricksBuilder {
  create(type: 'cardPayment', containerId: string, config: CardPaymentConfig): Promise<BrickController>;
}

interface CardPaymentConfig {
  initialization: { amount: number; payer?: { email?: string } };
  customization?: { paymentMethods?: { maxInstallments?: number } };
  callbacks: {
    onReady: () => void;
    onSubmit: (data: CardPaymentData) => Promise<void>;
    onError: (error: BrickError) => void;
  };
}

interface CardPaymentData {
  token: string;
  issuer_id: string;
  payment_method_id: string;
  transaction_amount: number;
  installments: number;
  payer: { email: string };
}

interface BrickError {
  type: string;
  cause: string;
}

interface BrickController {
  unmount(): void;
}

@Injectable({ providedIn: 'root' })
export class MpService {
  private mp: MercadoPagoInstance | null = null;
  private sdkLoaded = false;
  private activeBrick: BrickController | null = null;

  async loadSdk(): Promise<void> {
    if (this.sdkLoaded) return;
    await this.injectScript('https://sdk.mercadopago.com/js/v2');
    this.mp = new window.MercadoPago(environment.mpPublicKey, { locale: 'es-AR' });
    this.sdkLoaded = true;
  }

  async mountCardPaymentBrick(options: {
    containerId: string;
    amount: number;
    emailPagador: string;
    onSubmit: (data: CardPaymentData) => Promise<void>;
    onError?: (error: BrickError) => void;
  }): Promise<void> {
    await this.loadSdk();
    this.unmountBrick();

    const builder = this.mp!.bricks();
    this.activeBrick = await builder.create('cardPayment', options.containerId, {
      initialization: {
        amount: options.amount,
        payer:  { email: options.emailPagador },
      },
      customization: {
        paymentMethods: { maxInstallments: 1 },
      },
      callbacks: {
        onReady: () => {},
        onSubmit: options.onSubmit,
        onError:  options.onError ?? ((err) => console.error('MP Brick error:', err)),
      },
    });
  }

  unmountBrick(): void {
    if (this.activeBrick) {
      this.activeBrick.unmount();
      this.activeBrick = null;
    }
  }

  redirectToInitPoint(initPoint: string): void {
    window.location.href = initPoint;
  }

  private injectScript(src: string): Promise<void> {
    return new Promise((resolve, reject) => {
      if (document.querySelector(`script[src="${src}"]`)) {
        resolve();
        return;
      }
      const script = document.createElement('script');
      script.src = src;
      script.onload  = () => resolve();
      script.onerror = () => reject(new Error(`No se pudo cargar el SDK de Mercado Pago`));
      document.head.appendChild(script);
    });
  }
}
