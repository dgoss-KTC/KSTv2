import { useState } from 'react';
import { fireEvent, render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import type { WorkspaceAssignmentDto } from '../api/client';
import { WorkspaceTabBar } from './WorkspaceTabBar';

const workspaces: WorkspaceAssignmentDto[] = ['Alpha', 'Beta', 'Gamma'].map((displayName, sortOrder) => ({
  assignmentId: displayName, displayName, site: 'SW', sortOrder, parentParts: ['PARENT'],
  productLineFrom: null, productLineTo: null, isTemporary: false, coverageEndsOn: null, isEnabled: true,
}));

function setup(list = workspaces) {
  const handlers = { onSelect: vi.fn(), onAdd: vi.fn(), onManage: vi.fn(), onEdit: vi.fn(), onArchive: vi.fn(), onDelete: vi.fn(), onReorder: vi.fn(), onSelectGeneral: vi.fn() };
  function Harness() {
    const [ordered, setOrdered] = useState(list);
    return <><WorkspaceTabBar {...handlers} workspaces={ordered} activeId="Alpha" isGeneralActive={false}
      onReorder={(ids) => {
        handlers.onReorder(ids);
        setOrdered(ids.map(id => ordered.find(w => w.assignmentId === id)!));
      }} /><button>Outside destination</button></>;
  }
  return { user: userEvent.setup(), ...handlers, ...render(<Harness />) };
}
const trigger = (name = 'Alpha') => screen.getByRole('button', { name: `Workspace actions for ${name}` });
const item = (name: string) => within(screen.getByRole('menu')).getByRole('menuitem', { name });

describe('Workspace action menu — offline DOM interaction', () => {
  it.each(['{Enter}', ' ', '{ArrowDown}'])('keyboard entry %s focuses the first enabled item', async (key) => {
    const { user, onEdit } = setup();
    trigger().focus();
    await user.keyboard(key);
    expect(item('Edit Workspace')).toHaveFocus();
    expect(trigger()).toHaveAttribute('aria-controls', screen.getByRole('menu').id);
    expect(onEdit).not.toHaveBeenCalled();
    await user.keyboard('{Escape}');
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    expect(trigger()).toHaveFocus();
  });

  it('ArrowUp enters at the last item; arrows wrap and Home/End skip disabled left/right actions', async () => {
    const { user } = setup();
    trigger().focus(); await user.keyboard('{ArrowUp}');
    expect(item('Delete Permanently')).toHaveFocus();
    expect(item('Move Left')).toBeDisabled();
    await user.keyboard('{ArrowDown}'); expect(item('Edit Workspace')).toHaveFocus();
    await user.keyboard('{ArrowDown}'); expect(item('Move Right')).toHaveFocus();
    await user.keyboard('{Home}{ArrowUp}'); expect(item('Delete Permanently')).toHaveFocus();
    await user.keyboard('{Home}{End}'); expect(item('Delete Permanently')).toHaveFocus();
    await user.keyboard('{Escape}');
    trigger('Gamma').focus(); await user.keyboard('{ArrowDown}{ArrowDown}');
    expect(item('Move Left')).toHaveFocus();
    expect(item('Move Right')).toBeDisabled();
    await user.keyboard('{ArrowDown}'); expect(item('Archive Workspace')).toHaveFocus();
    await user.keyboard('{ArrowUp}'); expect(item('Move Left')).toHaveFocus();
  });

  it('with one workspace both disabled moves are skipped and cannot dispatch', async () => {
    const { user, onReorder } = setup(workspaces.slice(0, 1));
    await user.click(trigger());
    await user.keyboard('{ArrowDown}'); expect(item('Archive Workspace')).toHaveFocus();
    await user.keyboard('{ArrowUp}'); expect(item('Edit Workspace')).toHaveFocus();
    await user.click(item('Move Left')); await user.click(item('Move Right'));
    expect(onReorder).not.toHaveBeenCalled();
    expect(screen.getByRole('menu')).toBeInTheDocument();
  });

  it.each(['{Enter}', ' '])('activation %s invokes Edit once with the connected stable dialog-return trigger', async (key) => {
    const { user, onEdit, onArchive, onDelete } = setup();
    const owner = trigger(); owner.focus(); await user.keyboard('{ArrowDown}');
    await user.keyboard(key);
    expect(onEdit).toHaveBeenCalledExactlyOnceWith(workspaces[0], owner);
    expect(onArchive).not.toHaveBeenCalled(); expect(onDelete).not.toHaveBeenCalled();
    expect(owner.isConnected).toBe(true);
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
  });

  it.each([
    { name: 'Archive Workspace', key: '{Home}{ArrowUp}{ArrowUp}', handler: 'onArchive' as const },
    { name: 'Delete Permanently', key: '{End}', handler: 'onDelete' as const },
  ])('$name delegates focus to its existing dialog rather than reclaiming it', async ({ name, key, handler }) => {
    const result = setup();
    const owner = trigger();
    result[handler].mockImplementation(() => {
      // Stand-in for a dialog's synchronous focus acquisition; real dialogs are covered in lifecycle tests.
      screen.getByRole('button', { name: 'Outside destination' }).focus();
    });
    owner.focus(); await result.user.keyboard(`{ArrowDown}${key}`);
    expect(item(name)).toHaveFocus();
    await result.user.keyboard('{Enter}');
    expect(result[handler]).toHaveBeenCalledExactlyOnceWith(workspaces[0], owner);
    expect(screen.getByRole('button', { name: 'Outside destination' })).toHaveFocus();
  });

  it.each([false, true])('Tab dismisses with native progression from the trigger (shift=%s)', async (shift) => {
    const { user } = setup();
    trigger().focus(); await user.keyboard('{ArrowDown}{End}');
    // user-event's Tab default uses the ORIGINAL event target even if keydown moves focus;
    // a removed menu item makes it lose focus/throw. Verify uncancelled keydown separately,
    // then model native progression from the resulting live activeElement (Windows check pending).
    expect(fireEvent.keyDown(document.activeElement!, { key: 'Tab', shiftKey: shift })).toBe(true);
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    expect(trigger()).toHaveFocus();
    await user.tab({ shift });
    expect(shift ? screen.getByRole('tab', { name: 'Alpha' }) : screen.getByRole('tab', { name: 'Beta' })).toHaveFocus();
  });

  it.each(['Move Left', 'Move Right'])('%s reorders once and leaves focus on the connected moved trigger', async (action) => {
    const { user, onReorder } = setup();
    const owner = trigger('Beta'); owner.focus(); await user.keyboard('{ArrowDown}');
    await user.keyboard(action === 'Move Left' ? '{ArrowDown}' : '{ArrowDown}{ArrowDown}');
    expect(item(action)).toHaveFocus(); await user.keyboard(' ');
    const expected = action === 'Move Left' ? ['Beta', 'Alpha', 'Gamma'] : ['Alpha', 'Gamma', 'Beta'];
    expect(onReorder).toHaveBeenCalledExactlyOnceWith(expected);
    expect(screen.getAllByRole('tab').map(tab => tab.textContent)).toEqual([...expected, 'General']);
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    expect(owner.isConnected).toBe(true); expect(owner).toHaveFocus();
  });

  it('outside pointer dismissal preserves its clicked destination, including within the tab bar', async () => {
    const { user, onSelect } = setup();
    await user.click(trigger());
    const destination = screen.getByRole('button', { name: 'Outside destination' });
    await user.click(destination);
    expect(screen.queryByRole('menu')).not.toBeInTheDocument(); expect(destination).toHaveFocus();
    await user.click(trigger());
    const tab = screen.getByRole('tab', { name: 'Beta' }); await user.click(tab);
    expect(screen.queryByRole('menu')).not.toBeInTheDocument(); expect(tab).toHaveFocus();
    expect(onSelect).toHaveBeenCalledExactlyOnceWith('Beta');
    await user.click(trigger()); await user.click(trigger('Beta'));
    expect(screen.getAllByRole('menu')).toHaveLength(1);
    expect(screen.getByRole('menu')).toHaveAccessibleName('Actions for Beta');
  });

  it.each(['dialog', 'alertdialog', 'export'])('yields Escape and Tab to a higher-priority %s overlay', async (kind) => {
    const { user } = setup(); await user.click(trigger());
    const overlay = document.createElement('div');
    if (kind === 'export') overlay.setAttribute('data-analytical-export-menu', '');
    else { overlay.setAttribute('role', kind); overlay.setAttribute('aria-modal', 'true'); }
    const control = document.createElement('button'); overlay.append(control); document.body.append(overlay);
    try {
      control.focus();
      expect(fireEvent.keyDown(control, { key: 'Escape' })).toBe(true);
      expect(fireEvent.keyDown(control, { key: 'Tab' })).toBe(true);
      expect(control).toHaveFocus();
      expect(trigger()).toHaveAttribute('aria-expanded', 'true');
    } finally { overlay.remove(); }
  });
});
