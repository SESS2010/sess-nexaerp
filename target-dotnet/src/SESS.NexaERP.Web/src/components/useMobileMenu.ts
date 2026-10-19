import { useEffect, useRef, useState } from 'react'

export function useMobileMenu(routeKey: string) {
  const [narrow, setNarrow] = useState(() => window.matchMedia('(max-width: 800px)').matches)
  const [open, setOpen] = useState(false)
  const drawerRef = useRef<HTMLElement>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)
  const close = () => setOpen(false)
  useEffect(() => {
    const media = window.matchMedia('(max-width: 800px)')
    const change = () => { setNarrow(media.matches); setOpen(false) }
    media.addEventListener('change', change)
    return () => media.removeEventListener('change', change)
  }, [])
  useEffect(() => { setOpen(false) }, [routeKey])
  useEffect(() => {
    if (!narrow || !open) return
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const selector = 'a[href], button:not([disabled])'
    const focusable = () => Array.from(drawerRef.current?.querySelectorAll<HTMLElement>(selector) ?? [])
    focusable()[0]?.focus()
    const keydown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') { event.preventDefault(); close() }
      if (event.key === 'Tab') {
        const elements = focusable(), first = elements[0], last = elements.at(-1)
        if (!first) { event.preventDefault(); return }
        if (event.shiftKey && (document.activeElement === first || !drawerRef.current?.contains(document.activeElement))) { event.preventDefault(); last?.focus() }
        else if (!event.shiftKey && (document.activeElement === last || !drawerRef.current?.contains(document.activeElement))) { event.preventDefault(); first.focus() }
      }
    }
    document.addEventListener('keydown', keydown)
    return () => {
      document.removeEventListener('keydown', keydown)
      document.body.style.overflow = previousOverflow
      buttonRef.current?.focus()
    }
  }, [narrow, open])
  return { narrow, open, close, toggle: () => setOpen(value => !value), drawerRef, buttonRef }
}
