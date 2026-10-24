import { expect, test } from '@playwright/test';
test.use({ timezoneId: 'Europe/Warsaw' });
for (const language of ['en','pl']) {
 test('schedule greeting and shifts in ' + language, async ({ page }) => {
  await page.clock.install({time: new Date('2026-09-19T10:00:00Z')});
  await page.addInitScript(() => localStorage.setItem('gf3.auth.access-token','hero-test'));
  await page.route('**/*', async route => {
    const path=new URL(route.request().url()).pathname;
    if (!path.startsWith('/api/')) return route.continue();
    let body: unknown=[];
    if(path==='/api/auth/session') body={role:'employee',employeeId:12,userName:'oleg',displayName:'Oleh Protsun'};
    else if(path==='/api/account-language') body={language};
    else if(path==='/api/employee-ui-state') body={scheduleColumnOrders:{},readNotificationIds:[],pinnedSwapIds:[]};
    else if(path==='/api/employee-schedules') body=[{id:1,containerId:1,containerName:'Main',shopId:1,shopName:'Shop',name:'Front Desk',year:2026,month:9,publicationStatus:'public',employees:[{id:1,employeeId:12,firstName:'Oleh',lastName:'Protsun',displayName:'Oleh Protsun',displayOrder:0}],slots:[19,20].map(day=>({id:day,dayOfMonth:day,slotNo:1,employeeId:12,fromTime:'08:00',toTime:'16:00',status:'ASSIGNED'}))}];
    else if(path.includes('negotiate')) return route.fulfill({status:503});
    await route.fulfill({contentType:'application/json',body:JSON.stringify(body)});
  });
  await page.goto('/schedule');
  const hero=page.getByRole('region',{name:language==='en'?'Your upcoming shifts':'Twoje najbliższe zmiany'});
  await expect(hero.getByRole('heading',{level:1})).toHaveText(language==='en'?'Hey, Oleh':'Cześć, Oleh');
  await expect(hero.getByText(language==='en'?'Ends in 4h 0m':'Koniec za 4 godz. 0 min')).toBeVisible();
  await expect(hero.getByText(language==='en'?'Starts in 20h 0m':'Początek za 20 godz. 0 min')).toBeVisible();
  await expect(page.getByRole('button',{name:'Clock in'})).toHaveCount(0);
  for(const width of [320,390,768]) {
    await page.setViewportSize({width,height:844});
    await expect(hero).toBeVisible();
    expect(await hero.evaluate(el=>el.scrollWidth<=el.clientWidth)).toBe(true);
    const headings=hero.getByRole('heading',{level:2});
    const first=await headings.nth(0).boundingBox(); const second=await headings.nth(1).boundingBox();
    expect(Math.abs(first!.y-second!.y)).toBeLessThan(2);
    if(width===390) await page.screenshot({path:test.info().outputPath('schedule-hero-'+language+'.png'),fullPage:false});
  }
  await page.clock.fastForward(60_000);
  await expect(hero.getByText(language==='en'?'Ends in 3h 59m':'Koniec za 3 godz. 59 min')).toBeVisible();
  await page.clock.fastForward(24*60*60*1000);
  await expect(hero.getByText(language==='en'?'Day off':'Dzień wolny')).toBeVisible();
 });
}
