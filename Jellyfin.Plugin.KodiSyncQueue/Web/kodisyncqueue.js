const SyncConfigurationPage = {
    pluginUniqueId: '771e19d6-5385-4caf-b35c-28a0e865cf63'
};

export default function (view) {
    view.addEventListener('viewshow', function () {
        Dashboard.showLoadingMsg();
        const page = this;
        ApiClient.getPluginConfiguration(SyncConfigurationPage.pluginUniqueId).then(function (config) {
            page.querySelector('#txtRetDays').value = config.RetDays || '0';
            page.querySelector('#chkEnabled').checked = config.IsEnabled || false;
            page.querySelector('#tkMovies').checked = config.TkMovies || false;
            page.querySelector('#tkBoxSets').checked = config.TkBoxSets || false;
            page.querySelector('#tkTVShows').checked = config.TkTvShows || false;
            page.querySelector('#tkMusic').checked = config.TkMusic || false;
            page.querySelector('#tkMusicVideos').checked = config.TkMusicVideos || false;
            Dashboard.hideLoadingMsg();
        });
    });
    view.querySelector('#ksqConfigurationForm').addEventListener('submit', function (e) {
        Dashboard.showLoadingMsg();
        const form = this;
        ApiClient.getPluginConfiguration(SyncConfigurationPage.pluginUniqueId).then(function (config) {
            config.RetDays = form.querySelector('#txtRetDays').value;
            if (isNaN(config.RetDays) || parseInt(config.RetDays) < 0) {
                config.RetDays = '0';
            }
            config.IsEnabled = form.querySelector('#chkEnabled').checked;
            config.TkMovies = form.querySelector('#tkMovies').checked;
            config.TkTvShows = form.querySelector('#tkTVShows').checked;
            config.TkMusic = form.querySelector('#tkMusic').checked;
            config.TkMusicVideos = form.querySelector('#tkMusicVideos').checked;
            config.TkBoxSets = form.querySelector('#tkBoxSets').checked;

            ApiClient.updatePluginConfiguration(SyncConfigurationPage.pluginUniqueId, config).then(Dashboard.processPluginConfigurationUpdateResult);
        });
        e.preventDefault();
        return false;
    });
}
