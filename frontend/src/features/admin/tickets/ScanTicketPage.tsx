import { useEffect, useRef, useState } from 'react';
import { Alert, Button, Card, Descriptions, Input, Select, Space, Tag, Typography } from 'antd';
import { CameraOutlined, QrcodeOutlined } from '@ant-design/icons';
import type { AxiosError } from 'axios';
import dayjs from 'dayjs';
import jsQR from 'jsqr';
import {
  listEntryGates,
  listEvents,
  type EntryGateResponse,
} from '../../../services/catalog/catalogApi';
import { scanTicket, type TicketResponse } from '../../../services/ticketing/ticketingApi';
import { PageHeader } from '../../../components/common/layout/PageHeader';
import { toast } from '../../../components/common/feedback/toast';

interface ScanErrorBody {
  message?: string;
}

interface EventOption {
  id: string;
  title: string;
}

const ANY_GATE_OPTION = '__any__';

const STATUS_COLOR: Record<TicketResponse['status'], string> = {
  Issued: 'default',
  CheckedIn: 'success',
  Void: 'error',
};

/**
 * Gate scan: pick which event and (optionally) which physical gate this device represents, then
 * either paste/wedge-scan a ticket's token, or point a camera at its QR code. "Any gate" is an
 * unscoped supervisor scanner that bypasses a section's gate restriction, if it has one.
 *
 * Camera decoding prefers the native Barcode Detection API where available (hardware-accelerated,
 * no extra JS work) and falls back to jsQR (pure JS) elsewhere. For sustained, high-volume gate
 * throughput, a dedicated hardware handheld/turnstile scanner wired in as a keyboard-wedge device
 * (already supported by the token field below) remains the more reliable mechanism — the camera
 * path here is best suited to staff walking the line with a phone/tablet, not the primary answer
 * to extreme concurrent check-in volume (see ADR-0025).
 */
export function ScanTicketPage() {
  const [events, setEvents] = useState<EventOption[]>([]);
  const [eventsLoading, setEventsLoading] = useState(true);
  const [eventId, setEventId] = useState<string | undefined>();
  const [gates, setGates] = useState<EntryGateResponse[]>([]);
  const [gateId, setGateId] = useState<string>(ANY_GATE_OPTION);
  const [token, setToken] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<TicketResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [cameraActive, setCameraActive] = useState(false);
  const [cameraError, setCameraError] = useState<string | null>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const rafRef = useRef<number | null>(null);

  useEffect(() => {
    listEvents({ mine: true, page: 1, pageSize: 100 })
      .then((response) => setEvents(response.events.map((e) => ({ id: e.id, title: e.title }))))
      .catch(() => toast.error('Could not load your events.'))
      .finally(() => setEventsLoading(false));
  }, []);

  useEffect(() => {
    let cancelled = false;
    const fetchGates = eventId ? listEntryGates(eventId) : Promise.resolve([]);

    fetchGates
      .then((fetched) => {
        if (cancelled) {
          return;
        }
        setGateId(ANY_GATE_OPTION);
        setGates(fetched);
      })
      .catch(() => toast.error('Could not load this event’s entry gates.'));

    return () => {
      cancelled = true;
    };
  }, [eventId]);

  const stopCamera = () => {
    if (rafRef.current !== null) {
      cancelAnimationFrame(rafRef.current);
      rafRef.current = null;
    }
    streamRef.current?.getTracks().forEach((track) => track.stop());
    streamRef.current = null;
    setCameraActive(false);
  };

  // Stop the camera if the user navigates away mid-scan.
  useEffect(() => stopCamera, []);

  const handleScan = async (tokenOverride?: string) => {
    const scanToken = (tokenOverride ?? token).trim();
    if (!eventId || !scanToken) {
      return;
    }

    setSubmitting(true);
    setResult(null);
    setError(null);
    try {
      const ticket = await scanTicket(
        scanToken,
        eventId,
        gateId === ANY_GATE_OPTION ? undefined : gateId,
      );
      setResult(ticket);
      setToken('');
    } catch (caught) {
      const axiosError = caught as AxiosError<ScanErrorBody>;
      const status = axiosError.response?.status;
      const message = axiosError.response?.data?.message;

      if (status === 404) {
        setError(message ?? 'No ticket matches that token.');
      } else if (status === 409) {
        setError(message ?? 'This ticket has already been checked in (or voided).');
      } else {
        setError('Something went wrong. Please try again.');
      }
    } finally {
      setSubmitting(false);
    }
  };

  const onDecoded = (decodedValue: string) => {
    stopCamera();
    setToken(decodedValue);
    void handleScan(decodedValue);
  };

  const decodeWithBarcodeDetector = async () => {
    if (!videoRef.current || !streamRef.current) {
      return;
    }
    try {
      const detector = new BarcodeDetector({ formats: ['qr_code'] });
      const barcodes = await detector.detect(videoRef.current);
      if (barcodes.length > 0) {
        onDecoded(barcodes[0].rawValue);
        return;
      }
    } catch {
      // Transient detect failures (e.g. a mid-frame read) — keep trying.
    }
    rafRef.current = requestAnimationFrame(() => void decodeWithBarcodeDetector());
  };

  const decodeWithJsQr = () => {
    const video = videoRef.current;
    const canvas = canvasRef.current;
    if (video && canvas && video.readyState === video.HAVE_ENOUGH_DATA) {
      canvas.width = video.videoWidth;
      canvas.height = video.videoHeight;
      const context = canvas.getContext('2d');
      if (context) {
        context.drawImage(video, 0, 0, canvas.width, canvas.height);
        const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
        const decoded = jsQR(imageData.data, imageData.width, imageData.height);
        if (decoded?.data) {
          onDecoded(decoded.data);
          return;
        }
      }
    }
    rafRef.current = requestAnimationFrame(decodeWithJsQr);
  };

  const startCamera = async () => {
    setCameraError(null);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment' },
      });
      streamRef.current = stream;
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        await videoRef.current.play();
      }
      setCameraActive(true);

      if ('BarcodeDetector' in window) {
        void decodeWithBarcodeDetector();
      } else {
        rafRef.current = requestAnimationFrame(decodeWithJsQr);
      }
    } catch {
      setCameraError(
        'Could not access the camera — check permissions, or use the token field below.',
      );
    }
  };

  const toggleCamera = () => {
    if (cameraActive) {
      stopCamera();
    } else {
      void startCamera();
    }
  };

  return (
    <div style={{ maxWidth: 560, margin: '0 auto' }}>
      <PageHeader
        title="Scan tickets"
        description="Pick the event and gate this device is scanning for, then paste or scan a ticket's token to check it in."
      />
      <Card styles={{ body: { padding: 24 } }}>
        <Space direction="vertical" style={{ width: '100%', marginBottom: 20 }}>
          <Select
            placeholder="Select event"
            loading={eventsLoading}
            value={eventId}
            onChange={setEventId}
            options={events.map((e) => ({ value: e.id, label: e.title }))}
            style={{ width: '100%' }}
          />
          <Select
            placeholder="Gate"
            disabled={!eventId}
            value={eventId ? gateId : undefined}
            onChange={setGateId}
            options={[
              { value: ANY_GATE_OPTION, label: 'Any gate (supervisor)' },
              ...gates.map((gate) => ({ value: gate.id, label: gate.name })),
            ]}
            style={{ width: '100%' }}
          />
        </Space>

        <Button
          icon={<CameraOutlined />}
          disabled={!eventId}
          onClick={() => void toggleCamera()}
          style={{ width: '100%', marginBottom: 12 }}
        >
          {cameraActive ? 'Stop camera' : 'Scan with camera'}
        </Button>

        {cameraError && (
          <Alert type="warning" showIcon message={cameraError} style={{ marginBottom: 12 }} />
        )}

        <div style={{ display: cameraActive ? 'block' : 'none', marginBottom: 12 }}>
          <video
            ref={videoRef}
            muted
            playsInline
            style={{ width: '100%', borderRadius: 8, backgroundColor: '#000' }}
          />
        </div>
        <canvas ref={canvasRef} style={{ display: 'none' }} />

        <Space.Compact style={{ width: '100%', marginBottom: 20 }}>
          <Input
            size="large"
            prefix={<QrcodeOutlined />}
            placeholder="Ticket token"
            value={token}
            disabled={!eventId}
            onChange={(event) => setToken(event.target.value)}
            onPressEnter={() => void handleScan()}
            autoFocus
          />
          <Button
            type="primary"
            size="large"
            disabled={!eventId}
            loading={submitting}
            onClick={() => void handleScan()}
          >
            Check in
          </Button>
        </Space.Compact>

        {error && <Alert type="error" showIcon message={error} style={{ marginBottom: 20 }} />}

        {result && (
          <Alert
            type="success"
            showIcon
            message="Ticket checked in"
            description={
              <Descriptions column={1} size="small" style={{ marginTop: 8 }}>
                <Descriptions.Item label="Ticket">{result.id}</Descriptions.Item>
                <Descriptions.Item label="Status">
                  <Tag color={STATUS_COLOR[result.status]}>{result.status}</Tag>
                </Descriptions.Item>
                {result.checkedInAt && (
                  <Descriptions.Item label="Checked in at">
                    {dayjs(result.checkedInAt).format('MMM D, YYYY · h:mm:ss A')}
                  </Descriptions.Item>
                )}
              </Descriptions>
            }
          />
        )}

        {!error && !result && (
          <Typography.Text type="secondary">
            {eventId
              ? 'Waiting for a ticket token — scan a QR code or paste it above.'
              : 'Select an event to start scanning.'}
          </Typography.Text>
        )}
      </Card>
    </div>
  );
}
