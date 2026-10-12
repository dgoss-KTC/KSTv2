import { fireEvent, render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { OpenOrderReportInputs } from './OpenOrderReportInputs';
import { initialOor, oorOptional, oorPreferenceKey } from './openOrderReportPreferences';

describe('Open Order Report inputs', () => {
  beforeEach(() => localStorage.clear());
  it('starts blank with all supported sites, remembers only allowlisted optional fields in Python order', () => {
    expect(initialOor().site).toBe(''); expect(initialOor().optionalFields).toEqual([]);
    localStorage.setItem(oorPreferenceKey, JSON.stringify(['Unit Price', 'Allocated', 'SQL', 'Allocated']));
    const value = initialOor(); expect(value.optionalFields).toEqual(['Allocated', 'Unit Price']);
    expect(value.customerName).toBe(''); const changed = vi.fn(); render(<OpenOrderReportInputs value={value} onChange={changed} />);
    expect(screen.getByLabelText('Site')).toHaveValue(''); expect(screen.queryByRole('option', { name: 'KS' })).not.toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'MS' })).toBeInTheDocument();
    expect(screen.getAllByRole('checkbox').map(c => c.parentElement?.textContent)).toEqual(oorOptional);
    fireEvent.click(screen.getByLabelText('Customer Part'));
    expect(changed.mock.calls[0][0].optionalFields).toEqual(['Allocated', 'Customer Part', 'Unit Price']);
    expect(screen.getByRole('heading', { name: 'Customer and site' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Sales and product' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Due dates' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Columns' })).toBeInTheDocument();
  });
  it('shows paired-bound errors, accepts Unicode digits and does not reject descending product lines', () => {
    const { rerender } = render(<OpenOrderReportInputs value={{ ...initialOor(), productLineFrom: '0001', dueDateFrom: '10/1' }} onChange={vi.fn()} />);
    expect(screen.getAllByRole('alert')).toHaveLength(2);
    rerender(<OpenOrderReportInputs value={{ ...initialOor(), productLineFrom: '９９９９', productLineTo: '０００１' }} onChange={vi.fn()} />);
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });
});
