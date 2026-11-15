function safePushUrl(value) {
  if (typeof value !== "string") return "/notifications";
  if (["/schedule", "/availability", "/notifications"].includes(value)) return value;
  const match = /^\/swap\?swapId=([1-9][0-9]*)$/.exec(value);
  return match && Number.isSafeInteger(Number(match[1])) ? value : "/notifications";
}

self.addEventListener("push", event => {
  event.waitUntil((async () => {
    let payload;
    try {
      const text = event.data?.text();
      if (!text || new TextEncoder().encode(text).length > 3072) throw new Error("Invalid payload");
      payload = event.data.json();
      if (!payload || typeof payload.notificationId !== "string" || !payload.notificationId.trim() || payload.notificationId.length > 256 ||
          typeof payload.title !== "string" || !payload.title.trim() || payload.title.length > 200 ||
          typeof payload.body !== "string" || payload.body.length > 1200 ||
          !["schedule", "availability", "swap"].includes(payload.tone) ||
          safePushUrl(payload.url) !== payload.url) throw new Error("Invalid payload");
    } catch {
      payload = { notificationId: "gf-notice", title: "GF", body: "You have a notification in GF.", url: "/notifications" };
    }
    await self.registration.showNotification(payload.title, {
      body: payload.body, icon: "/icons/gf-icon-192.png", badge: "/icons/gf-icon-192.png",
      tag: payload.notificationId, renotify: false, data: { url: safePushUrl(payload.url) }
    });
  })());
});

self.addEventListener("notificationclick", event => {
  event.notification.close();
  event.waitUntil((async () => {
    const url = new URL(safePushUrl(event.notification.data?.url), self.location.origin).href;
    const windows = await self.clients.matchAll({ type: "window", includeUncontrolled: true });
    for (const client of windows) {
      if (new URL(client.url).origin !== self.location.origin || client.frameType === "nested") continue;
      try {
        const navigated = await client.navigate(url);
        if (navigated) { await navigated.focus(); return; }
      } catch { /* An existing window may have closed during navigation. */ }
    }
    await self.clients.openWindow(url);
  })());
});
