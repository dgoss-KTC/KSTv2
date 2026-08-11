import { useCallback, useEffect, useState } from 'react';
import type { PartDetailResponseDto } from '../api/client';
import { fetchPartDetail, toPartDetailApiError, type PartDetailApiError } from '../api/partDetailApi';

export interface PartDetailState {
  detail: PartDetailResponseDto | null;
  isLoading: boolean;
  error: PartDetailApiError | null;
}

/**
 * Loads Part Info lazily for the workspace's currently selected MPS parent part. Only re-fetches
 * when `assignmentId` or `partNumber` change — Due/Release basis, horizon, and fiscal display
 * changes are not inputs to this hook and never trigger a reload (per the accepted Stage 6 contract).
 * Passing `partNumber = null` clears state without calling the backend (no selection).
 */
export function usePartDetail(assignmentId: string, partNumber: string | null) {
  const [detail, setDetail] = useState<PartDetailResponseDto | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<PartDetailApiError | null>(null);

  const load = useCallback(async () => {
    if (partNumber === null) {
      setDetail(null);
      setError(null);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    try {
      const result = await fetchPartDetail(assignmentId, partNumber);
      setDetail(result);
      setError(null);
    } catch (err) {
      setDetail(null);
      setError(toPartDetailApiError(err) ?? { type: 'error', detail: 'Could not load part information. Try again.' });
    } finally {
      setIsLoading(false);
    }
  }, [assignmentId, partNumber]);

  useEffect(() => {
    const id = setTimeout(() => void load(), 0);
    return () => clearTimeout(id);
  }, [load]);

  return { detail, isLoading, error, retry: load };
}
