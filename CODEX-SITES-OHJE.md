# Space Attack — Sites-julkaisun valmistelu

> Sijaintihuomio: tämä on ohjekopio Unity-projektissa.
> Valmisteltu julkaisupohja: C:\Users\Koask\Documents\Codex\2026-10-02\jos-minua-pyydet-n-tekem-n\outputs\space-attack-sites
> Alla olevat suhteelliset polut ja npm-komennot viittaavat julkaisupohjaan, eivät Unity-projektin juureen.
> Olemassa oleva Sites project_id: appgprj_6abf931dedc8819191be601278294648
> Käytä tätä samaa Sites-projektia; uutta sivustoa ei tarvitse luoda.


Valmisteltu 2.10.2026. Sites-yhteys toimii ja Space Attack -sivusto on luotu.
Sivusto on yksityinen ja julkaisematon; pelibuildia tai pelattavaa linkkiä ei vielä ole.
Julkinen julkaisu on tilillä sallittu. Varsinainen Unity-projekti tehdään erikseen.

## Unityn build

Tee Web/WebGL-release-build. Suositus tälle julkaisupohjalle:

- **Development Build:** pois.
- **Publishing Settings → Compression Format:** Gzip.
- **Publishing Settings → Decompression Fallback:** päällä.
- **Enable Native C/C++ Multithreading:** pois, jos versiosi tarjoaa asetuksen eikä peli tarvitse sitä.

Fallback purkaa ladatut tiedostot selaimessa ja helpottaa staattista hostausta, mutta lataaminen on vähemmän tehokasta kuin palvelimen oikein määritellyllä natiivilla purkamisella.
Myös pakkaamaton build käy. Tavallinen `.gz`/`.br`-build ilman fallbackia ja monisäikeinen build vaativat erillisen palvelinotsakkeiden tarkistuksen.

Kopioi valmiin WebGL-exportin **koko sisältö** tämän projektin `build`-kansioon.
Säilytä tiedostojen nimet ja hakemistorakenne:

```text
space-attack-sites/
  .openai/hosting.json
  package.json
  scripts/check-build.mjs
  build/
    index.html
    Build/
      ...loader.js
      ...framework.js.unityweb
      ...wasm.unityweb
      ...data.unityweb
    TemplateData/       (jos Unity luo tämän)
    StreamingAssets/    (jos peli käyttää tätä)
```

Peli alkaa suoraan Unityn omasta `index.html`-tiedostosta. Tämä pohja ei lisää erillistä pelikäyttöliittymää.

## Tarkistus ja julkaisu, kun build on valmis

1. Aja julkaisuprojektin juuressa `npm run check`. Komento tarkistaa exportin perusrakenteen ja näyttää tiedostokoot. Se ei rakenna Unity-peliä eikä todista pelin toimivuutta. Tyhjän build-kansion tulee tuottaa virhe.
2. Tarkista valmis peli HTTP-palvelimella. Käyttäjä tekee tehtävän edellyttämän oman pelitestauksen, myös äänet ja näppäimistöohjauksen.
3. Jatka tämän projektin julkaisua Codexissa. Käytä `.openai/hosting.json`-tiedoston olemassa olevaa `project_id`-arvoa; älä luo uutta Sites-projektia.
4. Ennen version tallentamista ota käyttöön tämän hakemiston oma Git-repositorio. Sisällytä valittu Unity-export commitin sisältöön ja pushaa täsmälleen tämä tila Sitesin lähderepositorioon. Hae silloin uusi lyhytikäinen tunniste tarvittaessa; tunnisteita ei tallenneta tiedostoihin.
5. Käytä Sitesin `package-site.sh`-apuria buildin pakkaamiseen. Pidä kaikki väliaikaistiedostot nykyisen workspacen `work`-kansiossa. Staattinen build määrittyy manifestissa hakemistoon `build`; pakkaaja normalisoi sen arkistossa hakemistoon `dist`.
6. Tallenna tarkistettu versio ja julkaise se julkiseksi, kun käyttäjä pyytää valmiin pelin julkaisemista. Tarkista julkaisun lopputila sekä pelin, WebAssemblyn ja muiden tiedostojen latautuminen.
7. Testaa julkaistu linkki ilman kirjautumista. Käytä palautuksessa juuri arvioitavaksi tarkoitettua versiota ja säilytä sitä vastaava commit.

Buildin kokorajat, HTTP-otsakkeet ja todellinen Unity-yhteensopivuus voidaan varmistaa vasta varsinaisesta buildista ja julkaisusta. Tälle sivustolle ei ole vielä tallennettu versiota tai tehty julkaisua.

`PREPARATION-PROMPT.txt` sisältää tämän hosting-valmistelun käyttäjäpyynnön. Se ei korvaa koko pelityön promptilokia. Ajankäyttö ja oma reflektio kirjataan erikseen.

## Lähteet

- [OpenAI: Sites](https://learn.chatgpt.com/docs/sites?surface=app)
- [Unity 6: Web-buildin julkaisu ja Decompression Fallback](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-deploying.html)
- [Unity: palvelinasetukset ja monisäikeisyys](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/building-distribution/server-configuration-code-samples/web-server-config-nginx)
