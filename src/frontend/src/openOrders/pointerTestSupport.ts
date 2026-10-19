import { fireEvent } from '@testing-library/react';

/** JSDOM lacks native pointer capture/hit testing; supply event coordinates without HTML5 DnD. */
export function columnPointer(type: string, target: Element | Document | Window, x: number, y: number, pointerId = 1, button = 0) {
  const event = new Event(type, { bubbles: true, cancelable: true });
  Object.assign(event, { clientX: x, clientY: y, pointerId, button, isPrimary: true });
  fireEvent(target, event);
  return event;
}
