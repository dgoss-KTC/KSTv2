import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import App from '../App';
import type { SystemStatusResponse } from '../api/client';

const mockStatus: SystemStatusResponse = {
  applicationName: "Keytronic Scheduler's Toolbox",
  applicationVersion: '0.1.0',
  backendFramework: '.NET 10',
  backendInstanceId: 'test-instance-id',
  startedAt: '2026-07-28T12:00:00-07:00',
  currentTime: '2026-07-28T12:01:00-07:00',
  snapshot: {
    available: false,
    snapshotId: null,
    createdAt: null,
    status: 'notLoaded',
  },
  dataSources: [
    { name: 'QAD', status: 'notConfigured' },
    { name: 'Shortage Database', status: 'notConfigured' },
  ],
};

describe('App integration', () => {
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('shows starting state initially', () => {
    fetchMock.mockReturnValue(new Promise(() => {})); // never resolves
    render(<App />);
    // In starting state the badge shows "Starting backend…" or "Waiting for readiness…"
    expect(
      screen.queryByText(/starting backend/i) ?? screen.queryByText(/waiting/i),
    ).toBeTruthy();
  });

  it('renders backend status when connected', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => mockStatus,
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText('Connected')).toBeInTheDocument();
    });

    expect(screen.getByText('.NET 10')).toBeInTheDocument();
    expect(screen.getByText('test-instance-id')).toBeInTheDocument();
    expect(screen.getByText('QAD')).toBeInTheDocument();
    expect(screen.getByText('Shortage Database')).toBeInTheDocument();
  });

  it('shows backend unavailable when fetch fails with network error', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

    render(<App />);

    await waitFor(() => {
      expect(screen.getByText(/backend is unavailable/i)).toBeInTheDocument();
    });
  });

  it('shows retry button when unavailable', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'));

    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /retry connection/i })).toBeInTheDocument();
    });
  });

  it('retry button re-fetches status', async () => {
    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'));
    fetchMock.mockResolvedValueOnce({
      ok: true,
      json: async () => mockStatus,
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /retry connection/i })).toBeInTheDocument();
    });

    await userEvent.click(screen.getByRole('button', { name: /retry connection/i }));

    await waitFor(() => {
      expect(screen.getByText('Connected')).toBeInTheDocument();
    });
  });

  it('shows refresh button when connected', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => mockStatus,
    });

    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /refresh status/i })).toBeInTheDocument();
    });
  });

  it('shows data sources with notConfigured status', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => mockStatus,
    });

    render(<App />);

    await waitFor(() => {
      const statuses = screen.getAllByText('notConfigured');
      expect(statuses).toHaveLength(2);
    });
  });
});
