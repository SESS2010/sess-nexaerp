import type { ReactNode } from 'react'
import { SessLogo } from '../components/SessLogo'
import type { PrintAddress, PrintCompany, PrintParty } from './types'
import './print.css'

/** Text for a CSS string literal inside @page content. */
function cssString(value: string): string {
  return `"${value.replace(/\\/g, '\\\\').replace(/"/g, '\\"')}"`
}

/**
 * One printable A4 document. On screen it is drawn as a sheet; when printed,
 * the application shell (sidebar, topbar, anything marked .no-print) is hidden
 * and the sheet flows onto A4 pages with the footer repeated on each page.
 */
export function PrintSheet({ footerLabel, children }: { footerLabel: string; children: ReactNode }) {
  return (
    <div className="print-root">
      <style>{`@page { @bottom-left { content: ${cssString(footerLabel)}; } }`}</style>
      <article className="pd-sheet">{children}</article>
    </div>
  )
}

export function addressLines(address: PrintAddress): string[] {
  return [...address.lines, `${address.city} – ${address.pin}`]
}

export function stateLabel(address: PrintAddress): string {
  return `${address.state.name}, Code ${address.state.code}`
}

export function Letterhead({ company, title, subtitle }: { company: PrintCompany; title: string; subtitle?: string }) {
  return (
    <header className="pd-letterhead">
      <div className="pd-lh-main">
        <SessLogo className="pd-logo" />
        <div className="pd-lh-text">
          <div className="pd-lh-name">{company.legalName}</div>
          {company.tagline && <div className="pd-lh-tagline">{company.tagline}</div>}
          <div className="pd-lh-address">{addressLines(company.address).join(', ')}</div>
          <div className="pd-lh-ids">
            <span><b>GSTIN</b> {company.gstin}</span>
            <span><b>PAN</b> {company.pan}</span>
            <span><b>State</b> {stateLabel(company.address)}</span>
            {company.cin && <span><b>CIN</b> {company.cin}</span>}
          </div>
          {(company.phone || company.email) && (
            <div className="pd-lh-contact">
              {company.phone && <span>Ph: {company.phone}</span>}
              {company.email && <span>Email: {company.email}</span>}
            </div>
          )}
        </div>
      </div>
      <div className="pd-title">
        <span className="pd-title-text">{title}</span>
        {subtitle && <span className="pd-title-sub">{subtitle}</span>}
      </div>
    </header>
  )
}

/** Label / value pairs in a compact two-column grid. */
export function Facts({ rows }: { rows: Array<[string, ReactNode]> }) {
  return (
    <dl className="pd-facts">
      {rows.map(([label, value]) => (
        <div key={label} className="pd-fact">
          <dt>{label}</dt>
          <dd>{value}</dd>
        </div>
      ))}
    </dl>
  )
}

export function PartyBlock({ heading, party, showState = true }: { heading: string; party: PrintParty; showState?: boolean }) {
  return (
    <section className="pd-box pd-party">
      <h3 className="pd-box-head">{heading}</h3>
      <div className="pd-party-name">{party.name}</div>
      {addressLines(party.address).map((line) => <div key={line}>{line}</div>)}
      {showState && <div><b>State:</b> {stateLabel(party.address)}</div>}
      {party.gstin && <div><b>GSTIN:</b> {party.gstin}</div>}
      {(party.contactPerson || party.phone) && (
        <div><b>Contact:</b> {[party.contactPerson, party.phone].filter(Boolean).join(', ')}</div>
      )}
    </section>
  )
}

export function SignatureBox({ caption, name, note }: { caption: string; name?: string; note?: ReactNode }) {
  return (
    <div className="pd-sign">
      <div className="pd-sign-space">{note}</div>
      <div className="pd-sign-line">{caption}</div>
      {name && <div className="pd-sign-name">{name}</div>}
    </div>
  )
}
