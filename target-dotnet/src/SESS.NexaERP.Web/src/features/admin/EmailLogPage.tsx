// R1 uses manual PO email. The route and navigation use the admin.email View grant.
// No outbox, retry or test endpoint is called until the approved R1.1 release.
export function EmailLogPage() {
  return <div className="page">
    <div className="page-header"><h1>E-mail log</h1></div>
    <p>E-mail sending starts in R1.1</p>
    <p>For R1, Purchase prints or saves each purchase order as a PDF and e-mails it manually.</p>
  </div>
}
