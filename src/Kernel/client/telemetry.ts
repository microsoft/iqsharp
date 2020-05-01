// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="This function access is supposed to be public.")]
const CLIENT_INFO_API_URL = 'https://iqsharp-telemetry.azurewebsites.net/api/GetClientInfo?code=1kIsLwHdwLlH9n5LflhgVlafZaTH122yPK/3xezIjCQqC87MJrkdyQ==';

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
    FirstOrigin: string;
}

interface ConsentMarkup {
    Markup: string;
    Javascripts: string[];
    Stylesheets: string[];
}

interface MSCC {
    on(eventName: "consent", handler: ()=>void): void;
    hasConsent(): boolean;
    setConsent(): void;
}

declare var mscc: MSCC | null;

class CookieConsentHelper {
    private onConsentGranted: () => void;
    private mscc: MSCC;

    constructor(onConsentGranted: () => void) {
        this.onConsentGranted = onConsentGranted;
    }

    public setConsent() {
        if (this.mscc != null) {
            console.log("Consent granted by engagement.");
            this.mscc.setConsent();
        }
    }

    private addStylesheet(cssUrl: string): Promise<void> {
        return new Promise((resolve, reject) => {
            var style = document.createElement('link');
            style.onload = () => { resolve(); };
            style.href = cssUrl;
            style.type = 'text/css';
            style.rel = 'stylesheet';
            document.head.append(style);
        });
    }

    private addJavascript(javascriptUrl: string): Promise<void> {
        return new Promise((resolve, reject) => {
            var script = document.createElement('script');
            script.onload = () => { resolve(); };
            script.type = 'text/javascript';
            script.src = javascriptUrl;
            document.head.append(script);
        });
    }

    public async requestConsent(consentMarkup: ConsentMarkup) {
        console.log(`Adding Cookie API Stylesheets to the document: ${consentMarkup.Stylesheets.length} files`);
        await Promise.all(consentMarkup.Stylesheets.map((css, _, __) => this.addStylesheet(css)));
        console.log(`Cookie API Stylesheets loaded.`);

        var div = document.createElement('div');
        div.innerHTML = consentMarkup.Markup;
        document.body.prepend(div);
        console.log("Cookie API markup div added");

        console.log(`Adding Cookie API Javascripts to the document: ${consentMarkup.Javascripts.length} files`);
        await Promise.all(consentMarkup.Javascripts.map((javascript, _, __) => this.addJavascript(javascript)));
        console.log(`Cookie API Javascripts loaded.`);

        this.mscc = mscc;
        if (this.mscc == null) {
            console.log("Cookie API javascript was not loaded correctly. Consent cannot be obtained.");
            return;
        }

        console.log("Calling Cookie API hasConsent");
        this.mscc.on('consent', this.onConsentGranted);
        var hasConsent = this.mscc.hasConsent();
        console.log(`HasConsent: ${hasConsent}`);
        if (hasConsent) {
            this.onConsentGranted();
        }
    }
}

class TelemetryHelper {
    private cookieConsentHelper: CookieConsentHelper;

    public origin: string;

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
        var url =
            CLIENT_INFO_API_URL
            + (setHasConsent ? "&hasconsent=1" : "")
            + ((this.origin != null && this.origin != "") ? "&origin=" + this.origin : "");
        try {
            var clientInfo = await this.fetchExt<ClientInfo>(url);
            console.log(`ClientInfo: ${JSON.stringify(clientInfo)}`);
            return clientInfo;
        } catch (ex) {
            console.log(`ClientInfo not available. Unable to fetch : ${url}.`);
            return null;
        }
    }

    private async consentGranted() {
        console.log("Consent granted from the client");
        var clientInfo = await this.getClientInfoAsync(true);
        if (clientInfo != null) {
            this._clientInfoAvailable.trigger(clientInfo);
        }
    }

    public async initAsync() {
        var clientInfo = await this.getClientInfoAsync();
        if (clientInfo != null) {
            if (!clientInfo.HasConsent
                && clientInfo.CookieConsentMarkup != null) {
                this.cookieConsentHelper.requestConsent(clientInfo.CookieConsentMarkup);
            }
            else {
                this._clientInfoAvailable.trigger(clientInfo);
            }
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