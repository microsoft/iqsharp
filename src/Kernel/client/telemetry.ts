// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

//[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="This function access is supposed to be public.")]
const CLIENT_INFO_API_URL = 'https://iqsharp-telemetry.azurewebsites.net/api/GetClientInfo?code=1kIsLwHdwLlH9n5LflhgVlafZaTH122yPK/3xezIjCQqC87MJrkdyQ==';

const COOKIE_CONSENT_JS = 'https://wcpstatic.microsoft.com/mscc/lib/v2/wcp-consent.js';

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
    HasConsent: boolean;
    FirstOrigin: string;
}

enum consentCategories {
    Required = "Required",
    Analytics = "Analytics",
    SocialMedia = "SocialMedia",
    Advertising = "Advertising"
}
enum Themes {
    light = "light",
    dark = "dark",
    highContrast = "high-contrast"
}

interface SiteConsent {
    readonly isConsentRequired: boolean;
    getConsent(): Record<consentCategories, boolean>;
    getConsentFor(consentCategory: consentCategories): boolean;
    manageConsent(): void;
    onConsentChanged(callbackMethod: (newConsent: Record<consentCategories, boolean>) => void);
}

interface WcpConsentInterface {
    init(culture: string, placeholderIdOrElement: string | HTMLElement, initCallback?: (err?: Error, siteConsent?: SiteConsent) => void, onConsentChanged?: (newConsent: Record<consentCategories, boolean>) => void, theme?: Themes): void;
}

declare let WcpConsent: WcpConsentInterface | null;

class CookieConsentHelper {
    private onConsentGranted: () => void;
    private siteConsent: SiteConsent;

    constructor(onConsentGranted: () => void) {
        this.onConsentGranted = onConsentGranted;
    }

    private addJavascript(javascriptUrl: string): Promise<void> {
        return new Promise((resolve) => {
            const script = document.createElement('script');
            script.onload = () => { resolve(); };
            script.type = 'text/javascript';
            script.src = javascriptUrl;
            document.head.append(script);
        });
    }

    private onConsentChanged(categoryPreferences: Record<consentCategories, boolean>) {
        console.log("onConsentChanged", categoryPreferences);
        this.checkRequiredConsent();
    }

    private checkRequiredConsent() {
        const hasConsent = this.siteConsent.getConsentFor(consentCategories.Required);
        console.log(`HasConsent: ${hasConsent}`);
        if (hasConsent) {
            this.onConsentGranted();
        }
    }

    public async requestConsent() {
        const div = document.createElement('div');
        div.id = "cookie-banner";
        document.body.prepend(div);
        console.log("Cookie banner div added");

        console.log(`Adding Cookie API Javascript to the document`);
        await this.addJavascript(COOKIE_CONSENT_JS);
        console.log(`Cookie API Javascripts loaded.`);

        if (WcpConsent === null) {
            console.log("Cookie API javascript was not loaded correctly. Consent cannot be obtained.");
            return;
        }

        console.log("Initializing WcpConsent...");
        const userLanguage = navigator.language;
        WcpConsent && WcpConsent.init(userLanguage, div.id, (err, siteConsent) => {
            if (!err) {
                this.siteConsent = siteConsent;
                console.log("WcpConsent initialized.");
                this.checkRequiredConsent();
            } else {
                console.log("Error initializing WcpConsent: " + err);
            }
        }, this.onConsentChanged, Themes.light);
    }
}

class TelemetryHelper {
    private cookieConsentHelper: CookieConsentHelper;

    public origin: string;

    constructor() {
        this.cookieConsentHelper = new CookieConsentHelper(() => { this.consentGranted() });
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
            console.error(`Error fetching url ${request.toString()}`)
            throw ex;
        }
    }

    private async getClientInfoAsync(setHasConsent = false): Promise<ClientInfo> {
        console.log("Getting ClientInfo");
        const url =
            CLIENT_INFO_API_URL
            + (setHasConsent ? "&hasconsent=1" : "")
            + ((this.origin !== null && this.origin !== "") ? "&origin=" + this.origin : "");
        try {
            const clientInfo = await this.fetchExt<ClientInfo>(url);
            console.log(`ClientInfo: ${JSON.stringify(clientInfo)}`);
            return clientInfo;
        } catch (ex) {
            console.log(`ClientInfo not available. Unable to fetch : ${url}.`);
            return null;
        }
    }

    private async consentGranted() {
        console.log("Consent granted from the client");
        const clientInfo = await this.getClientInfoAsync(true);
        if (clientInfo !== null) {
            this._clientInfoAvailable.trigger(clientInfo);
        }
    }

    public async initAsync() {
        const clientInfo = await this.getClientInfoAsync();
        if (clientInfo !== null) {
            if (!clientInfo.HasConsent) {
                this.cookieConsentHelper.requestConsent();
            }
            else {
                this._clientInfoAvailable.trigger(clientInfo);
            }
        }
    }

    public get clientInfoAvailable() { return this._clientInfoAvailable.expose(); }
    private readonly _clientInfoAvailable = new LiteEvent<ClientInfo>();
}

export const Telemetry = new TelemetryHelper();
export default Telemetry;