// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//TODO: Update to production URL
const CLIENT_INFO_API_URL = 'https://iqsharp-telemetry-test.azurewebsites.net/api/GetClientInfo?code=1kIsLwHdwLlH9n5LflhgVlafZaTH122yPK/3xezIjCQqC87MJrkdyQ==';

/*
There are npm packages for this but I preferred to keep it small
and simple with no additional dependencies
Code from https://stackoverflow.com/questions/12881212/does-typescript-support-events-on-classes
*/
export interface ILiteEvent<T> {
    on(handler: { (data?: T): void }): void;
    off(handler: { (data?: T): void }): void;
}

class LiteEvent<T> implements ILiteEvent<T> {
    private handlers: { (data?: T): void; }[] = [];

    public on(handler: { (data?: T): void }): void {
        this.handlers.push(handler);
    }

    public off(handler: { (data?: T): void }): void {
        this.handlers = this.handlers.filter(h => h !== handler);
    }

    public trigger(data?: T) {
        this.handlers.slice(0).forEach(h => h(data));
    }

    public expose(): ILiteEvent<T> {
        return this;
    }
}



export interface ClientInfo {
    Id: string;
    IsNew: boolean;
    CountryCode: string;
    CookieConsentMarkup: ConsentMarkup;
    HasConsent: boolean;
}

interface ConsentMarkup {
    Markup: string;
    Javascripts: string[];
    Stylesheets: string[];
}

interface MSCC {
    on(eventName: string, handler: Function): void;
    hasConsent(): boolean;
    setConsent(): void;
}

declare var mscc: MSCC | null;

class CookieConsentHelper {
    private consentMarkup: ConsentMarkup;
    private onConsentGranted: Function;
    private countCssLoaded: number;
    private countJsLoaded: number;
    private mscc: MSCC;

    constructor(onConsentGranted: Function) {
        this.onConsentGranted = onConsentGranted;
    }

    public setConsent() {
        if (this.mscc != null) {
            console.log("Consent granted by engagement.");
            this.mscc.setConsent();
        }
    }

    public requestConsent(consentMarkup: ConsentMarkup) {
        this.consentMarkup = consentMarkup;
        var consentHelper = this;
        consentHelper.countCssLoaded = 0;
        console.log(`Adding Cookie API CSSs to the document: ${consentHelper.consentMarkup.Stylesheets.length} files`);
        for (let css of consentHelper.consentMarkup.Stylesheets) {
            var style = document.createElement('link');
            style.onload = function () {
                consentHelper.onCssLoaded(consentHelper);
            };
            style.href = css;
            style.type = 'text/css';
            style.rel = 'stylesheet';
            document.head.append(style);
        }
    }

    private onCssLoaded(consentHelper: CookieConsentHelper) {
        consentHelper.countCssLoaded++;
        console.log(`Cookie API CSS loaded (${consentHelper.countCssLoaded}/${consentHelper.consentMarkup.Stylesheets.length})`);
        if (consentHelper.countCssLoaded == consentHelper.consentMarkup.Stylesheets.length) {
            var div = document.createElement('div');
            div.innerHTML = consentHelper.consentMarkup.Markup;
            document.body.prepend(div);
            console.log("Cookie API markup div added");

            consentHelper.countJsLoaded = 0;
            console.log(`Adding Cookie API JSs to the document: ${consentHelper.consentMarkup.Javascripts.length} files`);
            for (let js of consentHelper.consentMarkup.Javascripts) {
                var script = document.createElement('script');
                script.onload = function () {
                    consentHelper.onJsLoaded(consentHelper);
                };
                script.type = 'text/javascript';
                script.src = js;
                document.head.append(script);
            }
        }
    }

    private onJsLoaded(consentHelper: CookieConsentHelper) {
        consentHelper.countJsLoaded++;
        console.log(`Cookie API JS loaded (${consentHelper.countJsLoaded}/${consentHelper.consentMarkup.Javascripts.length})`);
        if (consentHelper.countJsLoaded == consentHelper.consentMarkup.Javascripts.length) {
            console.log("Calling Cookie API hasConsent");
            this.mscc = mscc;
            this.mscc.on('consent', consentHelper.onConsentGranted);
            var hasConsent = this.mscc.hasConsent();
            console.log(`HasConsent: ${hasConsent}`);
            if (hasConsent) {
                consentHelper.onConsentGranted();
            }
        }
    }
}

class TelemetryHelper {
    private cookieConsentHelper: CookieConsentHelper;

    constructor() {
        var telemetryHelper = this;
        this.cookieConsentHelper = new CookieConsentHelper(function () { telemetryHelper.consentGranted() });
    }

    private async fetchExt<T>(
        request: RequestInfo
    ): Promise<T> {
        const response = await fetch(request, {
            credentials: 'include'
        });
        if (!response.ok) {
            throw new Error(response.statusText);
        }
        try {
            const body = await response.json();
            return body;
        } catch (ex) {
            throw ex;
        }
    }

    private async getClientInfoAsync(setHasConsent: boolean = false): Promise<ClientInfo> {
        console.log("Getting ClientInfo");
        var url = CLIENT_INFO_API_URL + (setHasConsent ? "&hasconsent=1" : "")
        var clientInfo = await this.fetchExt<ClientInfo>(url);
        console.log(`ClientInfo: ${JSON.stringify(clientInfo)}`);
        return clientInfo;
    }

    private async consentGranted() {
        console.log("Consent granted from the client");
        var clientInfo = await this.getClientInfoAsync(true);
        this._clientInfoAvailable.trigger(clientInfo);
    }

    public async initAsync() {
        var clientInfo = await this.getClientInfoAsync();
        if (!clientInfo.HasConsent
            && clientInfo.CookieConsentMarkup != null) {
            this.cookieConsentHelper.requestConsent(clientInfo.CookieConsentMarkup);
        }
        else {
            this._clientInfoAvailable.trigger(clientInfo);
        }
    }

    public async registerEngagement() {
        this.cookieConsentHelper.setConsent();
    }

    public get clientInfoAvailable() { return this._clientInfoAvailable.expose(); }
    private readonly _clientInfoAvailable = new LiteEvent<ClientInfo>();
}

export const Telemetry = new TelemetryHelper();
export default Telemetry;