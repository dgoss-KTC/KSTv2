import { describe, it, expect, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ComponentInfoModal } from './ComponentInfoModal';
import type { ComponentDetailResponseDto } from '../api/client';

function makeDetail(overrides: Partial<ComponentDetailResponseDto> = {}): ComponentDetailResponseDto {
  return {
    site: 'SW',
    componentPart: 'COMP-1',
    description: 'Component One',
    partStatusCode: 'A',
    partStatusDescription: 'Active',
    iosCode: 'I',
    netQuantityOnHand: 10,
    nonNetQuantityOnHand: 0,
    standardCost: 12.5,
    qctc: 13.75,
    timeFence: 5,
    safetyTime: 2,
    safetyStock: 3,
    buyerPlanner: 'JDOE',
    purchaseLeadTimeDays: 7,
    inspectionLeadTimeDays: 1,
    cumulativeLeadTimeDays: 8,
    minimumOrderQuantity: 100,
    orderMultiple: 25,
    loadedAtUtc: '2026-08-13T12:00:00Z',
    isStale: false,
    warning: null,
    ...overrides,
  };
}

function renderModal(props: Partial<React.ComponentProps<typeof ComponentInfoModal>> = {}) {
  return render(
    <ComponentInfoModal
      componentPart="COMP-1"
      detail={makeDetail()}
      isLoading={false}
      error={null}
      onRetry={vi.fn()}
      onClose={vi.fn()}
      {...props}
    />,
  );
}

describe('ComponentInfoModal', () => {
  it('renders as an accessible dialog with the component identity', () => {
    renderModal();
    const dialog = screen.getByRole('dialog');
    expect(dialog).toHaveAttribute('aria-modal', 'true');
    expect(within(dialog).getByText('COMP-1')).toBeInTheDocument();
    expect(within(dialog).getByText('Component One')).toBeInTheDocument();
  });

  it('shows a loading state before detail arrives', () => {
    renderModal({ detail: null, isLoading: true });
    expect(screen.getByText(/loading component information/i)).toBeInTheDocument();
  });

  it('renders all accepted field groups from the contract', () => {
    renderModal();
    expect(screen.getByText('Net QOH')).toBeInTheDocument();
    expect(screen.getByText('Non-Net QOH')).toBeInTheDocument();
    expect(screen.getByText('Standard Cost')).toBeInTheDocument();
    expect(screen.getByText('QCTC')).toBeInTheDocument();
    expect(screen.getByText('Time Fence')).toBeInTheDocument();
    expect(screen.getByText('Safety Time')).toBeInTheDocument();
    expect(screen.getByText('Safety Stock')).toBeInTheDocument();
    expect(screen.getByText('Buyer / Planner')).toBeInTheDocument();
    expect(screen.getByText('Purchase LT')).toBeInTheDocument();
    expect(screen.getByText('Inspect LT')).toBeInTheDocument();
    expect(screen.getByText('Cumulative LT')).toBeInTheDocument();
    expect(screen.getByText('Min Order')).toBeInTheDocument();
    expect(screen.getByText('Order Multiple')).toBeInTheDocument();
    expect(screen.getByText('Part Status')).toBeInTheDocument();
    expect(screen.getByText('IOS')).toBeInTheDocument();
    expect(screen.getByText('A \u2014 Active')).toBeInTheDocument();
  });

  it('displays null planning values as the accepted No Data marker, not zero', () => {
    renderModal({
      detail: makeDetail({ timeFence: null, safetyTime: null, safetyStock: null, buyerPlanner: null }),
    });
    // Four independent null fields should each show the em dash marker.
    expect(screen.getAllByText('\u2014').length).toBeGreaterThanOrEqual(4);
  });

  it('does not render a null Standard Cost as $0.00', () => {
    renderModal({ detail: makeDetail({ standardCost: null }) });
    expect(screen.queryByText('$0')).not.toBeInTheDocument();
    expect(screen.queryByText(/^\$0(\.00)?$/)).not.toBeInTheDocument();
  });

  it('does not render a null QCTC as $0.00', () => {
    renderModal({ detail: makeDetail({ qctc: null }) });
    expect(screen.queryByText(/^\$0(\.00)?$/)).not.toBeInTheDocument();
  });

  describe('cost precision (exactly four decimal places)', () => {
    it('rounds Standard Cost for display: 0.286832 -> $0.2868', () => {
      renderModal({ detail: makeDetail({ standardCost: 0.286832 }) });
      expect(screen.getByText('$0.2868')).toBeInTheDocument();
    });

    it('pads QCTC for display: 0.259 -> $0.2590', () => {
      renderModal({ detail: makeDetail({ qctc: 0.259 }) });
      expect(screen.getByText('$0.2590')).toBeInTheDocument();
    });

    it('renders numeric zero cost as $0.0000, not No Data', () => {
      renderModal({ detail: makeDetail({ standardCost: 0, qctc: 0 }) });
      expect(screen.getAllByText('$0.0000')).toHaveLength(2);
    });

    it('renders null cost as the No Data marker, not $0.0000', () => {
      renderModal({ detail: makeDetail({ standardCost: null, qctc: null }) });
      expect(screen.queryByText('$0.0000')).not.toBeInTheDocument();
      expect(screen.getAllByText('\u2014').length).toBeGreaterThanOrEqual(2);
    });

    it('formats a string-valued numeric field identically to its number equivalent', () => {
      renderModal({ detail: makeDetail({ standardCost: '0.286832', qctc: '0.259' }) });
      expect(screen.getByText('$0.2868')).toBeInTheDocument();
      expect(screen.getByText('$0.2590')).toBeInTheDocument();
    });
  });

  it('renders numeric zero inventory as zero, not No Data', () => {
    renderModal({ detail: makeDetail({ netQuantityOnHand: 0, nonNetQuantityOnHand: 0 }) });
    expect(screen.getAllByText('0')).not.toHaveLength(0);
  });

  it('shows the stale warning banner when isStale is true', () => {
    renderModal({ detail: makeDetail({ isStale: true, warning: 'Showing the last known component information.' }) });
    expect(screen.getByRole('alert')).toHaveTextContent('Showing the last known component information.');
  });

  it('renders Show MRP as visibly disabled and non-functional', async () => {
    const user = userEvent.setup();
    renderModal();
    const mrpButton = screen.getByRole('button', { name: /show mrp/i });
    expect(mrpButton).toBeDisabled();
    await user.click(mrpButton);
  });

  it('renders the future Inventory / Lot Locations placeholder with no request implied', () => {
    renderModal();
    expect(screen.getByText('Inventory / Lot Locations')).toBeInTheDocument();
    expect(screen.getByText(/inventory location detail will be added in a later stage/i)).toBeInTheDocument();
  });

  it('renders the future Approved Vendors placeholder', () => {
    renderModal();
    expect(screen.getByText('Approved Vendors')).toBeInTheDocument();
    expect(screen.getByText(/approved vendor detail will be added in a later stage/i)).toBeInTheDocument();
  });

  it('X closes the modal', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal({ onClose });
    await user.click(screen.getByRole('button', { name: /close component information/i }));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('Escape closes the modal', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal({ onClose });
    await user.keyboard('{Escape}');
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('Escape closes the modal when focus is on the Close button', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal({ onClose });
    screen.getByRole('button', { name: /close component information/i }).focus();
    await user.keyboard('{Escape}');
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('Escape closes the modal when focus is on another focusable control inside the modal', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal({
      detail: null,
      error: { type: 'error', detail: 'Database currently unavailable.' },
      onClose,
    });
    screen.getByRole('button', { name: /retry/i }).focus();
    await user.keyboard('{Escape}');
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('repeated open/close cycles do not leave duplicate or stale Escape handlers', async () => {
    const user = userEvent.setup();
    const onCloseFirst = vi.fn();
    const { unmount } = renderModal({ onClose: onCloseFirst });
    unmount();

    const onCloseSecond = vi.fn();
    renderModal({ onClose: onCloseSecond });
    await user.keyboard('{Escape}');
    expect(onCloseSecond).toHaveBeenCalledTimes(1);
    expect(onCloseFirst).not.toHaveBeenCalled();
  });

  it('after the modal unmounts, pressing Escape does not trigger its close callback', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const { unmount } = renderModal({ onClose });
    unmount();
    await user.keyboard('{Escape}');
    expect(onClose).not.toHaveBeenCalled();
  });

  it('clicking the backdrop does not close the modal', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    const { container } = renderModal({ onClose });
    const backdrop = container.querySelector('.component-info-modal-backdrop');
    expect(backdrop).not.toBeNull();
    if (backdrop) await user.click(backdrop);
    expect(onClose).not.toHaveBeenCalled();
  });

  it('renders a retryable error state that keeps the modal open', async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();
    const onClose = vi.fn();
    renderModal({
      detail: null,
      error: { type: 'error', detail: 'Database currently unavailable.' },
      onRetry,
      onClose,
    });
    expect(screen.getByText('Database currently unavailable.')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /retry/i }));
    expect(onRetry).toHaveBeenCalledTimes(1);
    expect(onClose).not.toHaveBeenCalled();
  });

  it('Close is available and works from the error state', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderModal({
      detail: null,
      error: { type: 'error', detail: 'Database currently unavailable.' },
      onClose,
    });
    await user.click(screen.getByRole('button', { name: /^close$/i }));
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('initial focus lands on the Close button', () => {
    renderModal();
    expect(screen.getByRole('button', { name: /close component information/i })).toHaveFocus();
  });

  it('Tab from the last focusable element wraps to the first (focus trap)', async () => {
    const user = userEvent.setup();
    renderModal();
    const dialog = screen.getByRole('dialog');
    const focusable = within(dialog).getAllByRole('button');
    focusable[focusable.length - 1].focus();
    await user.tab();
    expect(focusable[0]).toHaveFocus();
  });
});
